import { Application } from "pixi.js";
import {
  BattleBuffType,
  BattleCommandType,
  BattleEventType,
  BattleTeam,
  decodeEventBatch,
  encodeCommandBatch,
  fixRaw,
} from "./BattleProtocol";
import {
  BattleUnitMetadata,
  BattleView,
  setBattleViewAssetBaseUrl,
} from "./BattleView";

type LeanClrModule = {
  HEAPU8: Uint8Array;
  _malloc(size: number): number;
  ccall(
    name: string,
    returnType: string,
    argTypes: string[],
    args: unknown[],
  ): unknown;
  cwrap(
    name: string,
    returnType: string,
    argTypes: string[],
  ): (...args: unknown[]) => unknown;
  setValue(ptr: number, value: number, type: string): void;
  load_assembly_file?: (
    namePtr: number,
    extensionPtr: number,
    bufPtr: number,
    sizePtr: number,
  ) => number;
  __battleSetupBatch?: Uint8Array;
  __battleInputBatch?: Uint8Array | null;
  __battleOutputBatch?: Uint8Array | null;
};

declare const createStartupModule: (
  config?: Record<string, unknown>,
) => Promise<LeanClrModule>;

type BattleMountOptions = {
  setupBytes?: Uint8Array | number[];
  heroes?: BattleHeroMountMetadata[];
  enemies?: BattleHeroMountMetadata[];
  assetBaseUrl?: string;
  onExit?: {
    invokeMethodAsync(methodName: string): Promise<void>;
  };
  onBattleEnded?: {
    invokeMethodAsync(methodName: string, victory: boolean): Promise<void>;
  };
};

type BattleHeroMountMetadata = {
  unitId: number;
  name?: string;
  portraitUrl?: string;
  unitAssetId?: string;
  stage?: number;
  star?: number;
};

class XunyaoAdventureBattleViewApp {
  private readonly app = new Application();
  private readonly battleView = new BattleView();
  private readonly unitMetadata = new Map<number, BattleUnitMetadata>();
  private readonly assemblyCache = new Map<string, Uint8Array>();
  private readonly container: HTMLElement;
  private readonly setupBytes: Uint8Array | null;
  private readonly heroes: BattleHeroMountMetadata[];
  private readonly enemies: BattleHeroMountMetadata[];
  private readonly assetBaseUrl: string;
  private readonly onExit: BattleMountOptions["onExit"];
  private readonly onBattleEnded: BattleMountOptions["onBattleEnded"];
  private module: LeanClrModule | null = null;
  private wasmBinary: Uint8Array | null = null;
  private managedAssemblyPtr: number | null = null;
  private battleFrameNo = 0;
  private battleTicking = false;
  private battlePaused = false;
  private pendingCommands: {
    unitId: number;
    type: BattleCommandType;
    targetUnitId?: number;
  }[] = [];
  private readonly logicTickIntervalMs = 100;
  private readonly fixedDeltaRaw = fixRaw(1 / 10);
  private tickTimer = 0;
  private tickerCallback: ((ticker: { deltaMS: number }) => void) | null = null;
  private battleEndNotified = false;

  constructor(container: HTMLElement, options: BattleMountOptions = {}) {
    this.container = container;
    this.assetBaseUrl = this.normalizeBaseUrl(
      options.assetBaseUrl ??
        new URL(/* @vite-ignore */ "../", import.meta.url).toString(),
    );
    this.setupBytes = options.setupBytes
      ? new Uint8Array(options.setupBytes)
      : null;
    this.heroes = Array.isArray(options.heroes) ? options.heroes : [];
    this.enemies = Array.isArray(options.enemies) ? options.enemies : [];
    this.onExit = options.onExit;
    this.onBattleEnded = options.onBattleEnded;
  }

  async start() {
    setBattleViewAssetBaseUrl(this.assetBaseUrl);
    await this.app.init({ background: "#0b0f14", resizeTo: this.container });
    this.container.replaceChildren(this.app.canvas);
    await BattleView.preloadAssets();
    this.battleView.mount(this.app);

    console.log("Booting LeanCLR from Pixi...");
    await this.loadLeanScript();
    await this.loadAssemblies();
    await this.initLeanClr();
    await this.prepareBattleSetup();
    this.battleView.setPauseHandlers(
      () => this.pause(),
      () => this.resume(),
      () => this.exit(),
    );
    await this.runManagedEntry();
    this.startBattleLoop();
  }

  private loadLeanScript() {
    if (typeof createStartupModule === "function") {
      return Promise.resolve();
    }

    return this.loadTextPreferBrotli("lean.js", (text) =>
      text.includes("createStartupModule"),
    ).then((scriptText) => {
      const script = document.createElement("script");
      script.text = `${scriptText}\n//# sourceURL=lean.js`;
      document.head.appendChild(script);
    });
  }

  private async loadAssemblies() {
    const files = [
      "mscorlib.dll.bytes",
      "netstandard.dll.bytes",
      "System.Numerics.dll.bytes",
      "XunyaoAdventure.Battle.Core.dll.bytes",
    ];
    for (const file of files) {
      this.assemblyCache.set(
        file,
        await this.loadBinaryPreferBrotli(file, this.isPortableExecutable),
      );
    }
    this.wasmBinary = await this.loadBinaryPreferBrotli(
      "lean.wasm",
      this.isWasmBinary,
    );
  }

  private async initLeanClr() {
    const module = await createStartupModule({
      print: (text: string) => console.log(text),
      printErr: (text: string) => console.error(text),
      locateFile: (path: string) => path,
      wasmBinary: this.wasmBinary,
    });

    this.module = module;
    module.load_assembly_file = (
      namePtr: number,
      extensionPtr: number,
      bufPtr: number,
      sizePtr: number,
    ) => {
      const name = this.readCString(namePtr);
      const extension = this.readCString(extensionPtr);
      const key = extension ? `${name}.${extension}.bytes` : `${name}.bytes`;
      const data = this.assemblyCache.get(key);
      if (!data) {
        return 1;
      }

      const ptr = module._malloc(data.length);
      module.HEAPU8.set(data, ptr);
      module.setValue(bufPtr, ptr, "*");
      module.setValue(sizePtr, data.length, "i32");
      return 0;
    };

    const result = module.ccall(
      "initialize_runtime",
      "number",
      [],
      [],
    ) as number;
    if (result !== 0) {
      throw new Error(`initialize_runtime failed: ${result}`);
    }
  }

  private async prepareBattleSetup() {
    if (!this.module) {
      throw new Error("LeanCLR module is not initialized");
    }

    const setup = await this.createBattleSetup();
    this.battleView.setUnitMetadata(this.unitMetadata);
    this.module.__battleSetupBatch = setup;
    this.module.__battleInputBatch = null;
    this.module.__battleOutputBatch = null;
    this.battleFrameNo = 0;
    this.battleView.reset();
  }

  private async runManagedEntry() {
    const code = this.invokeManagedMethod("App", "Main");
    if (code !== 0) {
      throw new Error(`invoke_method failed: ${code}`);
    }
    if (this.module) {
      this.module.__battleSetupBatch = undefined;
    }
    console.log("Managed bootstrap finished.");
  }

  private startBattleLoop() {
    if (this.battleTicking) {
      return;
    }

    this.battleTicking = true;
    this.tickerCallback = (ticker) => {
      const deltaMs = ticker.deltaMS;
      if (this.battlePaused) {
        return;
      }

      this.battleView.update(deltaMs);
      if (!this.battleTicking || this.battlePaused) {
        return;
      }

      this.tickTimer += deltaMs;
      while (this.tickTimer >= this.logicTickIntervalMs) {
        this.tickTimer -= this.logicTickIntervalMs;
        this.tickBattle();
      }
    };
    this.app.ticker.add(this.tickerCallback);
  }

  private tickBattle() {
    if (!this.module || !this.battleTicking) {
      return;
    }

    this.module.__battleInputBatch = encodeCommandBatch(
      this.battleFrameNo,
      this.fixedDeltaRaw,
      this.drainPendingCommands(),
    );
    const code = this.invokeManagedMethod("App", "Tick");
    if (code !== 0) {
      console.error(`App.Tick failed: ${code}`);
      this.battleTicking = false;
      return;
    }
    this.flushBattleOutput();
  }

  private flushBattleOutput() {
    if (!this.module || !this.module.__battleOutputBatch) {
      return;
    }

    const output = decodeEventBatch(this.module.__battleOutputBatch);
    this.battleFrameNo = output.frameNo + 1;
    this.logBattleEvents(output);
    this.battleView.renderFrame(output);
    this.module.__battleOutputBatch = null;
  }

  private async createBattleSetup() {
    if (!this.setupBytes) {
      throw new Error(
        "Battle setupBytes is required. Setup must be pushed by the server.",
      );
    }

    this.unitMetadata.clear();
    for (const hero of this.heroes) {
      this.unitMetadata.set(hero.unitId, {
        profileId: 0,
        profileName: "PlayerHero",
        roleName: hero.name ?? "Hero",
        portraitUrl: hero.portraitUrl,
        unitAssetId: hero.unitAssetId,
        stage: hero.stage,
        star: hero.star,
      });
    }
    for (const enemy of this.enemies) {
      this.unitMetadata.set(enemy.unitId, {
        profileId: 0,
        profileName: "ArenaOpponent",
        roleName: enemy.name ?? "Enemy",
        portraitUrl: enemy.portraitUrl,
        unitAssetId: enemy.unitAssetId,
        stage: enemy.stage,
        star: enemy.star,
      });
    }
    return this.setupBytes;
  }

  pause() {
    this.battlePaused = true;
    this.battleView.setPaused(true);
  }

  resume() {
    this.battlePaused = false;
    this.battleView.setPaused(false);
  }

  private exit() {
    if (this.onExit) {
      void this.onExit.invokeMethodAsync("ExitBattle");
      return;
    }

    window.location.href = "/";
  }

  private drainPendingCommands() {
    if (this.pendingCommands.length === 0) {
      return [];
    }
    const commands = this.pendingCommands;
    this.pendingCommands = [];
    return commands;
  }

  private invokeManagedMethod(typeName: string, methodName: string) {
    if (!this.module) {
      throw new Error("LeanCLR module is not initialized");
    }
    const assemblyPtr = this.getManagedAssemblyPtr();
    const invokeMethod = this.module.cwrap("invoke_method", "number", [
      "number",
      "string",
      "string",
    ]) as (assemblyPtr: number, typeName: string, methodName: string) => number;
    return invokeMethod(assemblyPtr, typeName, methodName);
  }

  private getManagedAssemblyPtr() {
    if (!this.module) {
      throw new Error("LeanCLR module is not initialized");
    }
    if (this.managedAssemblyPtr !== null) {
      return this.managedAssemblyPtr;
    }
    const loadAssembly = this.module.cwrap("load_assembly", "number", [
      "string",
    ]) as (name: string) => number;
    const assemblyPtr = loadAssembly("XunyaoAdventure.Battle.Core");
    if (!assemblyPtr) {
      throw new Error("load_assembly returned null");
    }
    this.managedAssemblyPtr = assemblyPtr;
    return assemblyPtr;
  }

  private readCString(ptr: number) {
    if (!this.module) {
      return "";
    }
    const heap = this.module.HEAPU8;
    let end = ptr;
    while (heap[end] !== 0) {
      end++;
    }
    return new TextDecoder("utf-8").decode(heap.slice(ptr, end));
  }

  private async loadBinaryPreferBrotli(
    file: string,
    isValid: (data: Uint8Array) => boolean,
  ) {
    const compressed = await this.tryFetchBinary(`${file}.br`);
    if (compressed) {
      if (isValid(compressed)) {
        console.log(`Loaded ${file}.br`);
        return compressed;
      }
      const decompressed = await this.tryDecompressBrotli(compressed);
      if (decompressed && isValid(decompressed)) {
        console.log(`Loaded ${file}.br (client decompressed)`);
        return decompressed;
      }
    }

    const data = await this.fetchBinary(file);
    if (!isValid(data)) {
      throw new Error(`Invalid binary content: ${file}`);
    }
    console.log(`Loaded ${file}`);
    return data;
  }

  private async loadTextPreferBrotli(
    file: string,
    isValid: (text: string) => boolean,
  ) {
    const compressed = await this.tryFetchBinary(`${file}.br`);
    if (compressed) {
      const decoded = this.decodeUtf8(compressed);
      if (isValid(decoded)) {
        console.log(`Loaded ${file}.br`);
        return decoded;
      }
      const decompressed = await this.tryDecompressBrotli(compressed);
      if (decompressed) {
        const decompressedText = this.decodeUtf8(decompressed);
        if (isValid(decompressedText)) {
          console.log(`Loaded ${file}.br (client decompressed)`);
          return decompressedText;
        }
      }
    }

    const text = await this.fetchText(file);
    if (!isValid(text)) {
      throw new Error(`Invalid text content: ${file}`);
    }
    console.log(`Loaded ${file}`);
    return text;
  }

  private async tryFetchBinary(file: string) {
    try {
      return await this.fetchBinary(file);
    } catch {
      return null;
    }
  }

  private async fetchBinary(file: string) {
    const response = await fetch(this.resolveAssetUrl(file));
    if (!response.ok) {
      throw new Error(`Failed to load ${file}: ${response.status}`);
    }
    return new Uint8Array(await response.arrayBuffer());
  }

  private async fetchText(file: string) {
    const response = await fetch(this.resolveAssetUrl(file));
    if (!response.ok) {
      throw new Error(`Failed to load ${file}: ${response.status}`);
    }
    return response.text();
  }

  private async tryDecompressBrotli(data: Uint8Array) {
    const DecompressionCtor = (
      globalThis as unknown as {
        DecompressionStream?: new (
          format: string,
        ) => TransformStream<Uint8Array, Uint8Array>;
      }
    ).DecompressionStream;
    if (!DecompressionCtor) {
      return null;
    }
    try {
      const stream = new Blob([data])
        .stream()
        .pipeThrough(new DecompressionCtor("br"));
      return new Uint8Array(await new Response(stream).arrayBuffer());
    } catch {
      return null;
    }
  }

  private decodeUtf8(data: Uint8Array) {
    return new TextDecoder("utf-8").decode(data);
  }

  private isPortableExecutable(data: Uint8Array) {
    return data.length > 2 && data[0] === 0x4d && data[1] === 0x5a;
  }

  private isWasmBinary(data: Uint8Array) {
    return (
      data.length > 4 &&
      data[0] === 0x00 &&
      data[1] === 0x61 &&
      data[2] === 0x73 &&
      data[3] === 0x6d
    );
  }

  dispose() {
    this.battleTicking = false;
    if (this.tickerCallback) {
      this.app.ticker.remove(this.tickerCallback);
      this.tickerCallback = null;
    }
    this.app.destroy(true, { children: true });
    this.container.replaceChildren();
  }

  private resolveAssetUrl(file: string) {
    if (/^https?:\/\//.test(file) || file.startsWith("/")) {
      return file;
    }

    return new URL(file, this.assetBaseUrl).toString();
  }

  private normalizeBaseUrl(assetBaseUrl: string) {
    const absoluteUrl = new URL(assetBaseUrl, window.location.href).toString();
    return absoluteUrl.endsWith("/") ? absoluteUrl : `${absoluteUrl}/`;
  }

  private logBattleEvents(output: ReturnType<typeof decodeEventBatch>) {
    for (const event of output.events) {
      if (
        event.type === BattleEventType.UnitMoved ||
        event.type === BattleEventType.UnitAttack
      ) {
        continue;
      }
      console.log(
        `[anim] f=${output.frameNo} ${BattleEventType[event.type]} unit=${event.unitId} target=${event.targetUnitId} team=${BattleTeam[event.team]} wave=${event.waveIndex} amount=${event.amountRaw} hp=${event.remainingHpRaw}${event.buffType ? ` buff=${BattleBuffType[event.buffType]}` : ""}`,
      );
    }
    if (output.battleEnded) {
      console.log(`[battle] ended winner=${BattleTeam[output.winner]}`);
      this.battleTicking = false;
      this.notifyBattleEnded(output.winner === BattleTeam.TeamA);
    }
  }

  private notifyBattleEnded(victory: boolean) {
    if (this.battleEndNotified) {
      return;
    }

    this.battleEndNotified = true;
    if (this.onBattleEnded) {
      window.setTimeout(() => {
        void this.onBattleEnded?.invokeMethodAsync("BattleEnded", victory);
      }, 1200);
    }
  }
}

const mountedBattles = new Map<number, XunyaoAdventureBattleViewApp>();
let nextMountedBattleId = 1;

export async function mountBattle(
  container: HTMLElement | string,
  options: BattleMountOptions = {},
) {
  const element =
    typeof container === "string"
      ? document.querySelector<HTMLElement>(container)
      : container;
  if (!element) {
    throw new Error("Battle mount container was not found.");
  }

  const app = new XunyaoAdventureBattleViewApp(element, options);
  await app.start();
  const id = nextMountedBattleId++;
  mountedBattles.set(id, app);
  return id;
}

export function disposeBattle(id: number) {
  const app = mountedBattles.get(id);
  if (!app) {
    return;
  }

  app.dispose();
  mountedBattles.delete(id);
}

export function pauseBattle(id: number) {
  mountedBattles.get(id)?.pause();
}

export function resumeBattle(id: number) {
  mountedBattles.get(id)?.resume();
}
