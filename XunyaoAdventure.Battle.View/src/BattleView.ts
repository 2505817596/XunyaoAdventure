import {
  Application,
  Assets,
  Container,
  Graphics,
  Sprite,
  Text,
  Texture,
} from "pixi.js";
import {
  BattleBuffType,
  BattleEventRecord,
  BattleEventType,
  BattleFrameOutput,
  BattleTeam,
  BattleUnitFrameState,
  fixToNumber,
} from "./BattleProtocol";
import { BattleSkillProfileNames } from "./BattleSetupData";

export enum BattleAnimName {
  Idle = "Idle",
  Run = "Run",
  Attack = "Attack",
  Hit = "Hit",
  Ultimate = "Ultimate",
  Recover = "Recover",
  Stun = "Stun",
  Heal = "Heal",
  Dead = "Dead",
}

export type BattleUnitMetadata = {
  profileId: number;
  profileName: string;
  roleName: string;
  portraitUrl?: string;
  unitAssetId?: string;
  stage?: number;
  star?: number;
};

type UnitRenderState = {
  unitId: number;
  team: BattleTeam;
  hpRaw: number;
  maxHpRaw: number;
  energyRaw: number;
  anim: BattleAnimName;
  dead: boolean;
  metadata: BattleUnitMetadata;
};

export class BattleView {
  private readonly baseWidth = 1334;
  private readonly baseHeight = 750;
  private readonly baseBottomHudHeight = 156;
  private readonly worldMinX = -1;
  private readonly worldMaxX = 9;
  private readonly worldMinY = -2;
  private readonly worldMaxY = 2;
  private readonly eventQueue: string[] = [];
  private readonly buffQueue: string[] = [];
  private readonly maxEventLines = 18;
  private readonly maxBuffLines = 8;
  private readonly unitViews = new Map<number, BattleUnitView>();
  private readonly heroPortraitViews = new Map<
    number,
    BattleHeroPortraitView
  >();
  private readonly projectileViews = new Map<number, BattleProjectileView>();
  private readonly floatTextViews: FloatingTextView[] = [];
  private readonly playerUnitOrder: number[] = [];
  private app: Application | null = null;
  private paused = false;
  private onPauseRequested: (() => void) | null = null;
  private onResumeRequested: (() => void) | null = null;
  private onExitRequested: (() => void) | null = null;
  private unitMetadata = new Map<number, BattleUnitMetadata>();
  private root: Container | null = null;
  private background: Graphics | null = null;
  private battlefieldPanel: Container | null = null;
  private pauseButton: BattleIconButton | null = null;
  private pauseOverlay: BattlePauseOverlay | null = null;
  private heroHudPanel: Container | null = null;
  private heroHudBackground: Graphics | null = null;
  private statusLabel: Text | null = null;
  private buffLabel: Text | null = null;
  private logLabel: Text | null = null;
  private waveLabel: Text | null = null;
  private battleEndedLabel: Text | null = null;
  private viewWidth = 0;
  private viewHeight = 0;
  private layoutScale = 1;
  private bottomHudHeight = this.baseBottomHudHeight;

  static preloadAssets() {
    return UnitFrameLibrary.preloadAll();
  }

  mount(app: Application) {
    if (this.root) {
      return;
    }

    this.app = app;
    const root = new Container();
    app.stage.addChild(root);
    this.root = root;

    this.background = new Graphics();
    root.addChild(this.background);

    const battlefieldPanel = new Container();
    root.addChild(battlefieldPanel);
    this.battlefieldPanel = battlefieldPanel;

    this.pauseButton = new BattleIconButton("pause", () =>
      this.onPauseRequested?.(),
    );
    root.addChild(this.pauseButton.root);

    this.waveLabel = this.createLabel("波次 1/3", 34, 0xfff2ce, 0, 20, 180);
    this.waveLabel.anchor.set(0.5, 0);
    root.addChild(this.waveLabel);

    this.battleEndedLabel = this.createLabel("", 34, 0xffd36b, 0, 92, 420);
    this.battleEndedLabel.anchor.set(0.5, 0);
    root.addChild(this.battleEndedLabel);

    const heroHudPanel = new Container();
    this.heroHudBackground = new Graphics();
    heroHudPanel.addChild(this.heroHudBackground);
    root.addChild(heroHudPanel);
    this.heroHudPanel = heroHudPanel;

    this.pauseOverlay = new BattlePauseOverlay(
      () => this.onExitRequested?.(),
      () => this.onResumeRequested?.(),
    );
    this.pauseOverlay.visible = false;
    root.addChild(this.pauseOverlay.root);
    this.layout();
  }

  reset() {
    for (const unitView of this.unitViews.values()) {
      unitView.destroy();
    }
    for (const portraitView of this.heroPortraitViews.values()) {
      portraitView.destroy();
    }
    for (const projectileView of this.projectileViews.values()) {
      projectileView.destroy();
    }
    for (const floatTextView of this.floatTextViews) {
      floatTextView.destroy();
    }

    this.unitViews.clear();
    this.heroPortraitViews.clear();
    this.projectileViews.clear();
    this.floatTextViews.length = 0;
    this.playerUnitOrder.length = 0;
    this.eventQueue.length = 0;
    this.buffQueue.length = 0;
    this.setStatusText("Battle ready");
    this.flushBuffLog();
    this.setWaveText("波次 1/3");
    this.setBattleEndedText("");
    this.flushLog();
  }

  setUnitMetadata(metadata: Map<number, BattleUnitMetadata>) {
    this.unitMetadata = new Map(metadata);
  }

  renderFrame(output: BattleFrameOutput) {
    if (output.events.length > 0 || output.battleEnded) {
      this.setStatusText(
        `[state] f=${output.frameNo} wave=${output.currentWaveIndex} A=${output.alivePlayerCount} B=${output.aliveEnemyCount} ended=${output.battleEnded} winner=${BattleTeam[output.winner]} hash=${output.stateHash}`,
      );
      this.setWaveText(`波次 ${output.currentWaveIndex + 1}/3`);
    }

    for (const event of output.events) {
      this.applyEvent(event);
    }
    this.applyUnitStates(output.unitStates);

    if (output.battleEnded) {
      this.setBattleEndedText(
        `${output.winner === BattleTeam.TeamA ? "Victory" : "Defeat"}`,
      );
    }
  }

  update(deltaMs: number) {
    this.layout();
    if (this.paused) {
      return;
    }

    for (const unitView of this.unitViews.values()) {
      unitView.update(deltaMs);
    }
    for (const projectileView of this.projectileViews.values()) {
      projectileView.update();
    }
    for (let i = this.floatTextViews.length - 1; i >= 0; i--) {
      if (!this.floatTextViews[i].update(deltaMs)) {
        this.floatTextViews.splice(i, 1);
      }
    }
  }

  setPauseHandlers(
    onPauseRequested: () => void,
    onResumeRequested: () => void,
    onExitRequested: () => void,
  ) {
    this.onPauseRequested = onPauseRequested;
    this.onResumeRequested = onResumeRequested;
    this.onExitRequested = onExitRequested;
  }

  setPaused(paused: boolean) {
    this.paused = paused;
    if (this.pauseOverlay) {
      this.pauseOverlay.visible = paused;
    }
  }

  private applyUnitStates(unitStates: BattleUnitFrameState[]) {
    for (const unitState of unitStates) {
      this.unitViews
        .get(unitState.unitId)
        ?.setFrameState(
          unitState.hpRaw,
          unitState.maxHpRaw,
          unitState.energyRaw,
          unitState.isAlive,
        );

      if (unitState.team === BattleTeam.TeamA) {
        this.heroPortraitViews.get(unitState.unitId)?.setFrameState(unitState);
      }
    }
  }

  private applyEvent(event: BattleEventRecord) {
    switch (event.type) {
      case BattleEventType.BattleStarted:
        this.pushLogLine("[view] battle start");
        break;
      case BattleEventType.WaveStarted:
        this.pushLogLine(
          `[view] wave ${event.waveIndex + 1} spawn after 3s wait`,
        );
        this.setAllAliveUnitsAnim(BattleAnimName.Idle);
        break;
      case BattleEventType.WaveEnded:
        this.pushLogLine(
          `[view] wave ${event.waveIndex + 1} cleared, team returns`,
        );
        break;
      case BattleEventType.UnitSpawned:
        this.spawnUnit(
          event.unitId,
          event.team,
          event.xRaw,
          event.yRaw,
          event.remainingHpRaw,
          event.amountRaw > 0 ? Math.round(fixToNumber(event.amountRaw)) : 0,
        );
        break;
      case BattleEventType.UnitMoved:
        this.moveUnit(event.unitId, event.xRaw, event.yRaw);
        this.setUnitAnim(event.unitId, BattleAnimName.Run);
        break;
      case BattleEventType.UnitAttack:
        this.setUnitAnim(event.unitId, BattleAnimName.Attack);
        break;
      case BattleEventType.UnitDamaged:
        this.applyUnitDamaged(event);
        break;
      case BattleEventType.UnitHealed:
        this.updateUnitHp(event.unitId, event.remainingHpRaw);
        this.setUnitAnim(event.unitId, BattleAnimName.Heal);
        this.spawnFloatingText(
          event.unitId,
          `+${fixToNumber(event.amountRaw).toFixed(0)}`,
          0x4ee87c,
        );
        break;
      case BattleEventType.UnitDied:
        this.setUnitDead(event.unitId);
        break;
      case BattleEventType.UnitRevived:
        this.reviveUnit(event.unitId, event.remainingHpRaw);
        break;
      case BattleEventType.UnitEnergyChanged:
        this.spawnFloatingText(
          event.unitId,
          `+${fixToNumber(event.amountRaw).toFixed(0)}`,
          0xffb642,
        );
        break;
      case BattleEventType.UltimateCast:
        this.setUnitAnim(event.unitId, BattleAnimName.Ultimate);
        break;
      case BattleEventType.UltimateHit:
        this.setUnitAnim(event.unitId, BattleAnimName.Recover);
        break;
      case BattleEventType.UltimateRecovered:
        this.setUnitAnim(event.unitId, BattleAnimName.Idle);
        break;
      case BattleEventType.BuffApplied:
        if (event.buffType === BattleBuffType.Stun) {
          this.setUnitAnim(event.unitId, BattleAnimName.Stun);
        } else if (event.buffType === BattleBuffType.PhysicalArmorUp) {
          this.setUnitAnim(event.unitId, BattleAnimName.Heal);
        }
        this.pushBuffLine(
          `[buff] ${BattleBuffType[event.buffType]} apply unit ${event.unitId}`,
        );
        break;
      case BattleEventType.BuffExpired:
        this.setUnitAnim(event.unitId, BattleAnimName.Idle);
        break;
      case BattleEventType.ProjectileSpawned:
        this.spawnProjectile(event.unitId, event.team, event.xRaw, event.yRaw);
        break;
      case BattleEventType.ProjectileMoved:
        this.moveProjectile(event.unitId, event.xRaw, event.yRaw);
        break;
      case BattleEventType.ProjectileHit:
        this.hitProjectile(event.unitId, event.xRaw, event.yRaw);
        break;
    }
  }

  private spawnUnit(
    unitId: number,
    team: BattleTeam,
    xRaw: number,
    yRaw: number,
    hpRaw: number,
    profileId = 0,
  ) {
    if (!this.battlefieldPanel) {
      return;
    }

    let unitView = this.unitViews.get(unitId);
    if (!unitView) {
      unitView = new BattleUnitView(unitId);
      this.unitViews.set(unitId, unitView);
      this.battlefieldPanel.addChild(unitView.root);
    }

    unitView.setState({
      unitId,
      team,
      hpRaw,
      maxHpRaw: hpRaw,
      energyRaw: 0,
      anim: BattleAnimName.Idle,
      dead: false,
      metadata:
        profileId > 0
          ? this.metadataForProfile(profileId)
          : this.getMetadata(unitId),
    });
    unitView.setScreenPosition(
      this.worldToScreenX(xRaw),
      this.worldToScreenY(yRaw),
    );
    if (profileId > 0) {
      this.unitMetadata.set(unitId, this.metadataForProfile(profileId));
    }
    if (team === BattleTeam.TeamA) {
      this.ensureHeroPortrait(unitId, unitView.state);
    }
  }

  private moveUnit(unitId: number, xRaw: number, yRaw: number) {
    this.unitViews
      .get(unitId)
      ?.setScreenPosition(this.worldToScreenX(xRaw), this.worldToScreenY(yRaw));
  }

  private updateUnitHp(unitId: number, hpRaw: number) {
    this.unitViews.get(unitId)?.setHp(hpRaw, true);
  }

  private applyUnitDamaged(event: BattleEventRecord) {
    const amount = fixToNumber(event.amountRaw);
    if (amount <= 0) {
      this.spawnFloatingText(event.unitId, "格挡", 0xd9ecff);
      return;
    }

    this.updateUnitHp(event.unitId, event.remainingHpRaw);
    this.setUnitAnim(event.unitId, BattleAnimName.Hit);
    this.spawnFloatingText(event.unitId, `-${amount.toFixed(0)}`, 0xff4d4d);
  }

  private setUnitAnim(unitId: number, anim: BattleAnimName) {
    this.unitViews.get(unitId)?.setAnim(anim);
  }

  private setUnitDead(unitId: number) {
    this.unitViews.get(unitId)?.setDead();
    this.heroPortraitViews.get(unitId)?.setDead();
  }

  private reviveUnit(unitId: number, hpRaw: number) {
    this.unitViews.get(unitId)?.revive(hpRaw);
  }

  private ensureHeroPortrait(unitId: number, state: UnitRenderState) {
    if (!this.heroHudPanel || this.heroPortraitViews.has(unitId)) {
      return;
    }

    const portrait = new BattleHeroPortraitView(unitId);
    portrait.setState(state);
    this.heroPortraitViews.set(unitId, portrait);
    this.playerUnitOrder.push(unitId);
    this.heroHudPanel.addChild(portrait.root);
    this.layoutHeroHud();
  }

  private setAllAliveUnitsAnim(anim: BattleAnimName) {
    for (const unitView of this.unitViews.values()) {
      if (!unitView.isDead) {
        unitView.setAnim(anim);
      }
    }
  }

  private spawnProjectile(
    projectileId: number,
    team: BattleTeam,
    xRaw: number,
    yRaw: number,
  ) {
    if (!this.battlefieldPanel) {
      return;
    }

    let projectileView = this.projectileViews.get(projectileId);
    if (!projectileView) {
      projectileView = new BattleProjectileView(team);
      this.projectileViews.set(projectileId, projectileView);
      this.battlefieldPanel.addChild(projectileView.root);
    }
    projectileView.setScreenPosition(
      this.worldToScreenX(xRaw),
      this.worldToScreenY(yRaw),
    );
  }

  private moveProjectile(projectileId: number, xRaw: number, yRaw: number) {
    this.projectileViews
      .get(projectileId)
      ?.setScreenPosition(this.worldToScreenX(xRaw), this.worldToScreenY(yRaw));
  }

  private hitProjectile(projectileId: number, xRaw: number, yRaw: number) {
    const projectileView = this.projectileViews.get(projectileId);
    if (!projectileView) {
      return;
    }

    projectileView.setScreenPosition(
      this.worldToScreenX(xRaw),
      this.worldToScreenY(yRaw),
    );
    projectileView.destroy();
    this.projectileViews.delete(projectileId);
  }

  private spawnFloatingText(unitId: number, text: string, color: number) {
    const unitView = this.unitViews.get(unitId);
    if (!unitView || !this.battlefieldPanel) {
      return;
    }

    const position = unitView.getFloatTextAnchor();
    const floatText = new FloatingTextView(text, color, position.x, position.y);
    this.battlefieldPanel.addChild(floatText.root);
    this.floatTextViews.push(floatText);
  }

  private getMetadata(unitId: number): BattleUnitMetadata {
    return (
      this.unitMetadata.get(unitId) ?? {
        profileId: 0,
        profileName: "Default",
        roleName: "Unknown",
      }
    );
  }

  private metadataForProfile(profileId: number): BattleUnitMetadata {
    return {
      profileId,
      profileName: BattleSkillProfileNames[profileId] ?? `Profile${profileId}`,
      roleName: "Summon",
    };
  }

  private pushLogLine(line: string) {
    this.eventQueue.push(line);
    while (this.eventQueue.length > this.maxEventLines) {
      this.eventQueue.shift();
    }
    this.flushLog();
  }

  private pushBuffLine(line: string) {
    this.buffQueue.push(line);
    while (this.buffQueue.length > this.maxBuffLines) {
      this.buffQueue.shift();
    }
    this.flushBuffLog();
  }

  private flushLog() {
    if (this.logLabel) {
      this.logLabel.text = this.eventQueue.join("\n");
    }
  }

  private flushBuffLog() {
    if (this.buffLabel) {
      this.buffLabel.text = this.buffQueue.join("\n");
    }
  }

  private setStatusText(text: string) {
    if (this.statusLabel) {
      this.statusLabel.text = text;
    }
  }

  private setWaveText(text: string) {
    if (this.waveLabel) {
      this.waveLabel.text = text;
    }
  }

  private setBattleEndedText(text: string) {
    if (this.battleEndedLabel) {
      this.battleEndedLabel.text = text;
    }
  }

  private layout() {
    if (!this.app || !this.root) {
      return;
    }

    const width = this.app.renderer.width;
    const height = this.app.renderer.height;
    if (width === this.viewWidth && height === this.viewHeight) {
      return;
    }

    this.viewWidth = width;
    this.viewHeight = height;
    this.layoutScale = this.getLayoutScale(width, height);
    this.bottomHudHeight = Math.round(
      this.baseBottomHudHeight * this.layoutScale,
    );
    this.drawBackground();
    if (this.waveLabel) {
      this.waveLabel.x = width / 2;
      this.waveLabel.y = Math.max(10, 20 * this.layoutScale);
      this.waveLabel.style.fontSize = Math.round(34 * this.layoutScale);
    }
    if (this.pauseButton) {
      this.pauseButton.setScale(this.layoutScale);
      this.pauseButton.setPosition(
        18 * this.layoutScale,
        18 * this.layoutScale,
      );
    }
    if (this.battleEndedLabel) {
      this.battleEndedLabel.x = width / 2;
      this.battleEndedLabel.y = 92 * this.layoutScale;
      this.battleEndedLabel.style.fontSize = Math.round(34 * this.layoutScale);
    }
    this.pauseOverlay?.layout(width, height, this.layoutScale);
    if (this.heroHudPanel) {
      this.heroHudPanel.x = 0;
      this.heroHudPanel.y = Math.max(0, height - this.bottomHudHeight);
    }
    this.layoutHeroHud();
  }

  private drawBackground() {
    if (!this.background) {
      return;
    }

    const battleHeight = this.getBattlefieldHeight();
    this.background
      .clear()
      .rect(0, 0, this.viewWidth, this.viewHeight)
      .fill(0x102018)
      .rect(0, 0, this.viewWidth, battleHeight)
      .fill(0x1b3527)
      .rect(0, battleHeight * 0.42, this.viewWidth, battleHeight * 0.58)
      .fill(0x51693e)
      .rect(0, battleHeight * 0.58, this.viewWidth, battleHeight * 0.24)
      .fill({ color: 0x8a7c4f, alpha: 0.75 })
      .rect(0, battleHeight * 0.78, this.viewWidth, battleHeight * 0.22)
      .fill(0x4d8a45);
  }

  private layoutHeroHud() {
    if (!this.heroHudPanel || !this.heroHudBackground) {
      return;
    }

    const scale = this.layoutScale;
    const sidePadding = Math.max(14, 24 * scale);
    const panelWidth = Math.max(
      280 * scale,
      Math.min(this.viewWidth - sidePadding * 2, 720 * scale),
    );
    const panelHeight = 128 * scale;
    const panelX = (this.viewWidth - panelWidth) / 2;
    const panelY = 14 * scale;
    this.heroHudBackground
      .clear()
      .roundRect(panelX, panelY, panelWidth, panelHeight, 8 * scale)
      .fill({ color: 0x15210f, alpha: 0.78 })
      .stroke({ color: 0x46512f, width: Math.max(1, 2 * scale), alpha: 0.75 });

    const count = Math.max(1, this.playerUnitOrder.length);
    const slotWidth = Math.min(116 * scale, panelWidth / count);
    const totalWidth = slotWidth * count;
    const startX = panelX + (panelWidth - totalWidth) / 2 + slotWidth / 2;
    for (let i = 0; i < this.playerUnitOrder.length; i++) {
      const portrait = this.heroPortraitViews.get(this.playerUnitOrder[i]);
      if (!portrait) {
        continue;
      }
      portrait.setScale(scale);
      portrait.setPosition(startX + slotWidth * i, panelY + 16 * scale);
    }
  }

  private worldToScreenX(xRaw: number) {
    const x = fixToNumber(xRaw);
    const ratio = (x - this.worldMinX) / (this.worldMaxX - this.worldMinX);
    const padding = Math.max(28 * this.layoutScale, this.viewWidth * 0.055);
    return (
      padding + Math.max(0, Math.min(1, ratio)) * (this.viewWidth - padding * 2)
    );
  }

  private worldToScreenY(yRaw: number) {
    const y = fixToNumber(yRaw);
    const ratio = (y - this.worldMinY) / (this.worldMaxY - this.worldMinY);
    const padding = 36 * this.layoutScale;
    const battlefieldHeight = this.getBattlefieldHeight();
    return (
      padding +
      (1 - Math.max(0, Math.min(1, ratio))) * (battlefieldHeight - padding * 2)
    );
  }

  private getBattlefieldHeight() {
    return Math.max(
      280 * this.layoutScale,
      this.viewHeight - this.bottomHudHeight,
    );
  }

  private getLayoutScale(width: number, height: number) {
    const scale = Math.min(width / this.baseWidth, height / this.baseHeight);
    return Math.max(0.66, Math.min(1.18, scale));
  }

  private createLabel(
    text: string,
    fontSize: number,
    color: number,
    x: number,
    y: number,
    width = 360,
  ) {
    const label = new Text({
      text,
      style: {
        fill: color,
        fontFamily: "Arial",
        fontSize,
        wordWrap: true,
        wordWrapWidth: width,
      },
    });
    label.x = x;
    label.y = y;
    return label;
  }
}

class BattleUnitView {
  readonly root = new Container();
  private readonly shadow = new Graphics();
  private readonly avatar = new Sprite(Texture.EMPTY);
  private readonly damageHealthBar = new Graphics();
  private currentState: UnitRenderState;
  private damageHealthBarMs = 0;
  private animResetMs = 0;
  private frameElapsedMs = 0;
  private frameIndex = 0;
  private currentClip = "";
  private frames: Texture[] = [];
  private loop = true;

  constructor(unitId: number) {
    this.currentState = {
      unitId,
      team: BattleTeam.Neutral,
      hpRaw: 0,
      maxHpRaw: 0,
      energyRaw: 0,
      anim: BattleAnimName.Idle,
      dead: false,
      metadata: { profileId: 0, profileName: "Default", roleName: "Unknown" },
    };

    this.shadow.ellipse(0, 10, 28, 7).fill({ color: 0x000000, alpha: 0.38 });
    this.root.addChild(this.shadow);

    this.avatar.anchor.set(0.5, 0.88);
    this.avatar.scale.set(0.72);
    this.root.addChild(this.avatar);

    this.damageHealthBar.y = -94;
    this.damageHealthBar.visible = false;
    this.root.addChild(this.damageHealthBar);
  }

  get isDead() {
    return this.currentState.dead;
  }

  get state() {
    return this.currentState;
  }

  setState(state: UnitRenderState) {
    this.currentState = state;
    this.root.visible = true;
    this.root.alpha = 1;
    this.redrawDamageHealthBar();
    this.playClipForAnim(state.anim);
  }

  setScreenPosition(x: number, y: number) {
    this.root.position.set(x, y);
  }

  getFloatTextAnchor() {
    return { x: this.root.x, y: this.root.y - 56 };
  }

  setFrameState(
    hpRaw: number,
    maxHpRaw: number,
    energyRaw: number,
    isAlive: boolean,
  ) {
    this.currentState.hpRaw = hpRaw;
    this.currentState.maxHpRaw = Math.max(this.currentState.maxHpRaw, maxHpRaw);
    this.currentState.energyRaw = energyRaw;
    if (!isAlive) {
      this.currentState.dead = true;
      this.currentState.anim = BattleAnimName.Dead;
      this.root.visible = false;
    }
    this.redrawDamageHealthBar();
  }

  setHp(hpRaw: number, reveal = false) {
    this.currentState.hpRaw = hpRaw;
    if (reveal) {
      this.damageHealthBarMs = 2200;
      this.damageHealthBar.visible = true;
    }
    this.redrawDamageHealthBar();
  }

  setAnim(anim: BattleAnimName) {
    if (this.currentState.dead) {
      return;
    }
    this.currentState.anim = anim;
    this.playClipForAnim(anim);
    this.scheduleIdle(anim);
  }

  setDead() {
    this.currentState.dead = true;
    this.currentState.hpRaw = 0;
    this.currentState.anim = BattleAnimName.Dead;
    this.root.visible = false;
    this.damageHealthBar.visible = false;
    this.redrawDamageHealthBar();
  }

  revive(hpRaw: number) {
    this.currentState.dead = false;
    this.currentState.hpRaw = hpRaw;
    this.currentState.anim = BattleAnimName.Heal;
    this.root.visible = true;
    this.root.alpha = 1;
    this.damageHealthBarMs = 2200;
    this.damageHealthBar.visible = true;
    this.redrawDamageHealthBar();
    this.playClipForAnim(BattleAnimName.Heal);
    this.scheduleIdle(BattleAnimName.Heal);
  }

  update(deltaMs: number) {
    this.updateAnimation(deltaMs);
    this.updateAnimReset(deltaMs);
    this.updateDamageHealthBar(deltaMs);
  }

  destroy() {
    this.root.removeFromParent();
    this.root.destroy({ children: true });
  }

  private updateAnimation(deltaMs: number) {
    if (this.frames.length === 0) {
      return;
    }
    this.frameElapsedMs += deltaMs;
    if (this.frameElapsedMs < 90) {
      return;
    }
    this.frameElapsedMs = 0;

    if (this.frameIndex >= this.frames.length - 1) {
      if (!this.loop) {
        return;
      }
      this.frameIndex = 0;
    } else {
      this.frameIndex++;
    }
    this.avatar.texture = this.frames[this.frameIndex];
  }

  private updateAnimReset(deltaMs: number) {
    if (this.animResetMs <= 0 || this.currentState.dead) {
      return;
    }
    this.animResetMs -= deltaMs;
    if (this.animResetMs > 0) {
      return;
    }
    this.currentState.anim = BattleAnimName.Idle;
    this.playClipForAnim(BattleAnimName.Idle);
  }

  private updateDamageHealthBar(deltaMs: number) {
    if (this.damageHealthBarMs <= 0) {
      return;
    }

    this.damageHealthBarMs -= deltaMs;
    if (this.damageHealthBarMs <= 0) {
      this.damageHealthBar.visible = false;
    }
  }

  private scheduleIdle(anim: BattleAnimName) {
    if (anim === BattleAnimName.Dead || anim === BattleAnimName.Stun) {
      this.animResetMs = 0;
      return;
    }
    this.animResetMs =
      anim === BattleAnimName.Run
        ? 220
        : anim === BattleAnimName.Ultimate
          ? 700
          : 420;
  }

  private redrawDamageHealthBar() {
    const maxHpRaw = Math.max(1, this.currentState.maxHpRaw);
    const hpRatio = Math.max(
      0,
      Math.min(1, this.currentState.hpRaw / maxHpRaw),
    );
    const width = 58;
    const height = 7;
    this.damageHealthBar
      .clear()
      .roundRect(-width / 2 - 1, -1, width + 2, height + 2, 2)
      .fill({ color: 0x101010, alpha: 0.82 })
      .roundRect(-width / 2, 0, width, height, 2)
      .fill(0x481111)
      .roundRect(-width / 2, 0, width * hpRatio, height, 2)
      .fill(this.currentState.team === BattleTeam.TeamA ? 0x70ee55 : 0xf04d4d)
      .stroke({ color: 0xf3e4b8, width: 1, alpha: 0.75 });
  }

  private playClipForAnim(anim: BattleAnimName) {
    const unitAssetId = this.resolveUnitAssetId();
    const clip = this.resolveClipName(anim);
    const clipKey = `${unitAssetId}/${clip}`;
    if (this.currentClip === clipKey && anim === BattleAnimName.Run) {
      return;
    }

    this.currentClip = clipKey;
    this.frames = UnitFrameLibrary.getFrames(unitAssetId, clip);
    this.frameIndex = 0;
    this.frameElapsedMs = 0;
    this.loop = this.shouldLoop(anim);
    this.avatar.scale.x =
      this.currentState.team === BattleTeam.TeamB ? -0.72 : 0.72;
    if (this.frames.length > 0) {
      this.avatar.texture = this.frames[0];
    }
  }

  private resolveUnitAssetId() {
    return (
      this.currentState.metadata.unitAssetId ??
      (this.currentState.team === BattleTeam.TeamA ? "001" : "002")
    );
  }

  private resolveClipName(anim: BattleAnimName) {
    if (anim === BattleAnimName.Run) {
      return "run";
    }
    if (anim === BattleAnimName.Attack || anim === BattleAnimName.Ultimate) {
      return "skill0";
    }
    return "stand";
  }

  private shouldLoop(anim: BattleAnimName) {
    return (
      anim === BattleAnimName.Idle ||
      anim === BattleAnimName.Run ||
      anim === BattleAnimName.Stun
    );
  }
}

class BattleProjectileView {
  readonly root = new Graphics();

  constructor(team: BattleTeam) {
    const fill = team === BattleTeam.TeamA ? 0x72d7ff : 0xffb06b;
    this.root.circle(0, 0, 7).fill(fill).stroke({ color: 0xffffff, width: 2 });
    this.root.circle(0, 0, 3).fill(0xffffff);
  }

  setScreenPosition(x: number, y: number) {
    this.root.position.set(x, y);
  }

  update() {}

  destroy() {
    this.root.removeFromParent();
    this.root.destroy();
  }
}

type BattleIconKind = "pause" | "home" | "voice" | "music" | "play";

class BattleIconButton {
  readonly root = new Container();
  private readonly background = new Graphics();
  private readonly icon = new Graphics();
  private readonly label: Text | null;
  private readonly kind: BattleIconKind;
  private readonly onClick: () => void;
  private readonly size: number;

  constructor(
    kind: BattleIconKind,
    onClick: () => void,
    labelText = "",
    size = 76,
  ) {
    this.kind = kind;
    this.onClick = onClick;
    this.size = size;
    this.label = labelText
      ? new Text({
          text: labelText,
          style: {
            fill: 0xfff1b9,
            fontSize: 21,
            fontWeight: "800",
            stroke: { color: 0x3a2414, width: 4 },
          },
        })
      : null;

    this.root.eventMode = "static";
    this.root.cursor = "pointer";
    this.root.on("pointertap", () => this.onClick());
    this.root.addChild(this.background, this.icon);
    if (this.label) {
      this.label.anchor.set(0.5, 0);
      this.label.y = size + 8;
      this.root.addChild(this.label);
    }
    this.draw();
  }

  setPosition(x: number, y: number) {
    this.root.position.set(x, y);
  }

  setScale(scale: number) {
    this.root.scale.set(scale);
  }

  private draw() {
    const size = this.size;
    this.background
      .clear()
      .roundRect(0, 0, size, size, 8)
      .fill(0x5d432c)
      .stroke({ color: 0xf1d7a1, width: 4 })
      .roundRect(7, 7, size - 14, size - 14, 5)
      .fill(0x8f6441)
      .stroke({ color: 0x3b2618, width: 2 });

    this.icon.clear();
    const center = size / 2;
    const iconColor = 0xfff0a3;
    if (this.kind === "pause") {
      this.icon
        .roundRect(center - 13, center - 20, 9, 40, 2)
        .fill(iconColor)
        .roundRect(center + 5, center - 20, 9, 40, 2)
        .fill(iconColor);
    } else if (this.kind === "home") {
      this.icon
        .poly([
          center - 28,
          center - 4,
          center,
          center - 28,
          center + 28,
          center - 4,
          center + 21,
          center + 3,
          center,
          center - 15,
          center - 21,
          center + 3,
        ])
        .fill(iconColor)
        .rect(center - 20, center + 1, 40, 28)
        .fill(iconColor)
        .rect(center - 6, center + 13, 12, 16)
        .fill(0x8f6441);
    } else if (this.kind === "voice") {
      this.icon
        .poly([
          center - 27,
          center - 10,
          center - 12,
          center - 10,
          center + 9,
          center - 28,
          center + 9,
          center + 28,
          center - 12,
          center + 10,
          center - 27,
          center + 10,
        ])
        .fill(iconColor)
        .arc(center + 13, center, 13, -0.8, 0.8)
        .stroke({ color: iconColor, width: 5 })
        .arc(center + 18, center, 21, -0.7, 0.7)
        .stroke({ color: iconColor, width: 4 });
    } else if (this.kind === "music") {
      this.icon
        .rect(center - 1, center - 28, 10, 43)
        .fill(iconColor)
        .rect(center + 7, center - 28, 27, 8)
        .fill(iconColor)
        .circle(center - 7, center + 20, 13)
        .fill(iconColor);
    } else if (this.kind === "play") {
      this.icon
        .poly([
          center - 18,
          center - 27,
          center - 18,
          center + 27,
          center + 28,
          center,
        ])
        .fill(iconColor);
    }

    if (this.label) {
      this.label.x = center;
    }
  }
}

class BattlePauseOverlay {
  readonly root = new Container();
  private readonly dim = new Graphics();
  private readonly panel = new Graphics();
  private readonly buttons: BattleIconButton[];

  constructor(onExit: () => void, onResume: () => void) {
    this.buttons = [
      new BattleIconButton("home", onExit, "退出战斗"),
      new BattleIconButton("voice", () => {}, "背景声音：开"),
      new BattleIconButton("music", () => {}, "特效声音：开"),
      new BattleIconButton("play", onResume, "继续战斗"),
    ];
    this.root.addChild(this.dim, this.panel);
    for (const button of this.buttons) {
      this.root.addChild(button.root);
    }
  }

  get visible() {
    return this.root.visible;
  }

  set visible(value: boolean) {
    this.root.visible = value;
  }

  layout(width: number, height: number, scale: number) {
    this.dim
      .clear()
      .rect(0, 0, width, height)
      .fill({ color: 0x000000, alpha: 0.58 });

    const panelWidth = Math.max(
      360 * scale,
      Math.min(width - 48 * scale, 980 * scale),
    );
    const panelHeight = 228 * scale;
    const panelX = (width - panelWidth) / 2;
    const panelY = Math.max(
      42 * scale,
      (height - panelHeight) / 2 - 34 * scale,
    );
    this.panel
      .clear()
      .rect(panelX, panelY, panelWidth, panelHeight)
      .fill({ color: 0x2b251b, alpha: 0.68 })
      .stroke({ color: 0xd9a65b, width: Math.max(1, 2 * scale), alpha: 0.8 })
      .moveTo(panelX, panelY)
      .lineTo(panelX + panelWidth, panelY)
      .stroke({ color: 0xffd285, width: Math.max(1, 2 * scale), alpha: 0.62 })
      .moveTo(panelX, panelY + panelHeight)
      .lineTo(panelX + panelWidth, panelY + panelHeight)
      .stroke({ color: 0xffd285, width: Math.max(1, 2 * scale), alpha: 0.62 });

    const slotWidth = panelWidth / this.buttons.length;
    for (let i = 0; i < this.buttons.length; i++) {
      this.buttons[i].setScale(scale);
      this.buttons[i].setPosition(
        panelX + slotWidth * i + slotWidth / 2 - 38 * scale,
        panelY + 62 * scale,
      );
    }
  }
}

class BattleHeroPortraitView {
  readonly root = new Container();
  private readonly frame = new Graphics();
  private readonly portraitMask = new Graphics();
  private readonly portrait = new Sprite(Texture.EMPTY);
  private readonly stars = new Text({
    text: "★",
    style: {
      fill: 0xffd84c,
      fontSize: 17,
      fontWeight: "800",
      stroke: { color: 0x4b2a06, width: 3 },
    },
  });
  private readonly hpBar = new Graphics();
  private readonly energyBar = new Graphics();
  private readonly lockShade = new Graphics();
  private state: UnitRenderState;

  constructor(unitId: number) {
    this.state = {
      unitId,
      team: BattleTeam.TeamA,
      hpRaw: 0,
      maxHpRaw: 0,
      energyRaw: 0,
      anim: BattleAnimName.Idle,
      dead: false,
      metadata: { profileId: 0, profileName: "Default", roleName: "Hero" },
    };

    this.root.addChild(this.frame);
    this.portraitMask.roundRect(-42, 0, 84, 84, 3).fill(0xffffff);
    this.root.addChild(this.portraitMask);
    this.portrait.mask = this.portraitMask;
    this.portrait.anchor.set(0.5, 0.5);
    this.portrait.position.set(0, 42);
    this.portrait.scale.set(1.05);
    this.root.addChild(this.portrait);

    this.stars.anchor.set(0.5);
    this.stars.position.set(0, 76);
    this.root.addChild(this.stars, this.hpBar, this.energyBar, this.lockShade);
    this.redraw();
  }

  setState(state: UnitRenderState) {
    this.state = { ...state };
    this.portrait.texture = UnitFrameLibrary.getPortraitTexture(
      state.metadata.unitAssetId ??
        (state.team === BattleTeam.TeamA ? "001" : "002"),
    );
    this.redraw();
  }

  setFrameState(unitState: BattleUnitFrameState) {
    this.state.hpRaw = unitState.hpRaw;
    this.state.maxHpRaw = unitState.maxHpRaw;
    this.state.energyRaw = unitState.energyRaw;
    this.state.dead = !unitState.isAlive;
    this.redraw();
  }

  setDead() {
    this.state.dead = true;
    this.state.hpRaw = 0;
    this.redraw();
  }

  setPosition(x: number, y: number) {
    this.root.position.set(x, y);
  }

  setScale(scale: number) {
    this.root.scale.set(scale);
  }

  destroy() {
    this.root.removeFromParent();
    this.root.destroy({ children: true });
  }

  private redraw() {
    const hpRatio = this.ratio(this.state.hpRaw, this.state.maxHpRaw);
    const energyRatio = Math.max(
      0,
      Math.min(1, fixToNumber(this.state.energyRaw)),
    );
    const stageColor = getStageColor(this.state.metadata.stage ?? 1);
    const starCount = Math.max(1, Math.min(5, this.state.metadata.star ?? 1));
    this.stars.text = "★".repeat(starCount);
    this.frame
      .clear()
      .roundRect(-47, -5, 94, 94, 4)
      .fill(0x19120d)
      .stroke({ color: stageColor, width: 4 })
      .roundRect(-42, 0, 84, 84, 3)
      .fill(0x2b1d2d);

    this.hpBar
      .clear()
      .roundRect(-43, 100, 86, 8, 2)
      .fill(0x16210f)
      .roundRect(-42, 101, 84 * hpRatio, 6, 2)
      .fill(0x70e853);

    this.energyBar
      .clear()
      .roundRect(-43, 112, 86, 8, 2)
      .fill(0x1c2112)
      .roundRect(-42, 113, 84 * energyRatio, 6, 2)
      .fill(0xffb642);

    this.lockShade.clear();
    if (this.state.dead) {
      this.lockShade
        .roundRect(-42, 0, 84, 84, 3)
        .fill({ color: 0x000000, alpha: 0.58 });
      this.root.alpha = 0.78;
    } else {
      this.root.alpha = 1;
    }
  }

  private ratio(value: number, max: number) {
    if (max <= 0) {
      return 0;
    }
    return Math.max(0, Math.min(1, value / max));
  }
}

function getStageColor(stage: number) {
  switch (Math.max(1, Math.min(5, Math.round(stage)))) {
    case 2:
      return 0x8ed24d;
    case 3:
      return 0x42bce8;
    case 4:
      return 0xa66cff;
    case 5:
      return 0xf25b4f;
    default:
      return 0xf4e9d0;
  }
}

class FloatingTextView {
  readonly root = new Text({
    text: "",
    style: {
      fill: 0xffffff,
      fontSize: 28,
      fontWeight: "800",
      stroke: { color: 0x111111, width: 4 },
      dropShadow: { color: 0x000000, blur: 4, distance: 2 },
    },
  });
  private elapsedMs = 0;
  private readonly startX: number;
  private readonly startY: number;
  private readonly endX: number;
  private readonly endY: number;
  private readonly durationMs = 900;

  constructor(text: string, color: number, x: number, y: number) {
    this.startX = x;
    this.startY = y;
    this.endX = x + (Math.random() - 0.5) * 16;
    this.endY = y - 48 - Math.random() * 16;
    this.root.text = text;
    this.root.style.fill = color;
    this.root.anchor.set(0.5);
    this.root.position.set(x, y);
  }

  update(deltaMs: number) {
    this.elapsedMs += deltaMs;
    const t = Math.min(1, this.elapsedMs / this.durationMs);
    const eased = 1 - (1 - t) * (1 - t);
    this.root.x = this.startX + (this.endX - this.startX) * eased;
    this.root.y = this.startY + (this.endY - this.startY) * eased;
    this.root.alpha = 1 - t;
    if (t < 1) {
      return true;
    }
    this.destroy();
    return false;
  }

  destroy() {
    this.root.removeFromParent();
    this.root.destroy();
  }
}

class UnitFrameLibrary {
  private static assetBaseUrl = "/";
  private static readonly frameCounts: Record<string, Record<string, number>> =
    {
      "001": { stand: 8, run: 8, skill0: 12 },
      "002": { stand: 8, run: 8, skill0: 8 },
      "003": { stand: 12, run: 12, skill0: 12 },
    };

  static setAssetBaseUrl(assetBaseUrl: string) {
    UnitFrameLibrary.assetBaseUrl = assetBaseUrl.endsWith("/")
      ? assetBaseUrl
      : `${assetBaseUrl}/`;
  }

  static preloadAll() {
    return Assets.load(UnitFrameLibrary.getAllFramePaths());
  }

  static getFrames(unitAssetId: string, clip: string) {
    const frameCount = UnitFrameLibrary.frameCounts[unitAssetId]?.[clip] ?? 0;
    const frames: Texture[] = [];
    for (let index = 0; index < frameCount; index++) {
      frames.push(
        Texture.from(UnitFrameLibrary.getFramePath(unitAssetId, clip, index)),
      );
    }
    return frames;
  }

  static getPortraitTexture(unitAssetId: string) {
    return Texture.from(UnitFrameLibrary.getFramePath(unitAssetId, "stand", 0));
  }

  private static getAllFramePaths() {
    const paths: string[] = [];
    for (const unitAssetId of Object.keys(UnitFrameLibrary.frameCounts)) {
      const clips = UnitFrameLibrary.frameCounts[unitAssetId];
      for (const clip of Object.keys(clips)) {
        for (let index = 0; index < clips[clip]; index++) {
          paths.push(UnitFrameLibrary.getFramePath(unitAssetId, clip, index));
        }
      }
    }
    return paths;
  }

  private static getFramePath(
    unitAssetId: string,
    clip: string,
    index: number,
  ) {
    return `${UnitFrameLibrary.assetBaseUrl}unit/${unitAssetId}/${clip}/${index.toString().padStart(4, "0")}.png`;
  }
}

export function setBattleViewAssetBaseUrl(assetBaseUrl: string) {
  UnitFrameLibrary.setAssetBaseUrl(assetBaseUrl);
}
