namespace Amberstar.GameData;

public enum Message
{
    DropWhichItem,                    // 000: WELCHEN GEGENSTAND WEGWERFEN?
    ItemCannotBeDropped,              // 001: DU SOLLTEST DIESEN GEGENSTAND NICHT WEGWERFEN!
    UseWhichItem,                     // 002: WELCHEN GEGENSTAND BENUTZEN?
    ExamineWhichItem,                 // 003: WELCHEN GEGENSTAND ANSEHEN?
    TransferWhichItem,                // 004: WELCHEN GEGENSTAND ABGEBEN?
    NoMemberHasRoomForItem,           // 005: NIEMAND IN DER GRUPPE HAT DEN PLATZ ODER KANN ES TRAGEN!
    TransferHowManyGold,              // 006: WIEVIEL GOLD ABGEBEN?
    TransferHowManyFood,              // 007: WIEVIELE RATIONEN ABGEBEN?
    ItemNotEquippable,                // 008: DIESER GEGENSTAND KANN NICHT IN GEBRAUCH GENOMMEN WERDEN!
    WrongClass,                       // 009: FALSCHE KLASSE, UM DEN GEGENSTAND ZU GEBRAUCHEN!
    WrongGender,                      // 010: FALSCHES GESCHLECHT, UM DEN GEGENSTAND ZU GEBRAUCHEN!
    NotEnoughFreeHands,               // 011: ZU WENIG HÄNDE FREI, UM DEN GEGENSTAND ZU GEBRAUCHEN!
    NotEnoughFreeFingers,             // 012: ZU WENIG RINGFINGER FREI, UM DEN GEGENSTAND ZU GEBRAUCHEN!
    NoRoomForItem,                    // 013: KEIN PLATZ IM RUCKSACK FREI!
    GiveHowMuch,                      // 014: WIEVIEL ABGEBEN?
    HowManyItemsToDrop,               // 015: WIEVIEL WEGWERFEN?
    FlyingDiscNotUsableHere,          // 016: DIE FLUGSCHEIBE KANN HIER NICHT GEBRAUCHT WERDEN!
    ItemIsCursed,                     // 017: DIESER GEGENSTAND IST VERFLUCHT!
    ItemFulfillsSpecialPurposeNow,    // 018: DIESER GEGENSTAND ERFÜLLT NUN EINEN SPEZIELLEN ZWECK!
    WhomToTransferTo,                 // 019: WEM WILLST DU ES GEBEN?
    NotEquippableDuringFight,         // 020: KANN IM KAMPF NICHT IN GEBRAUCH GENOMMEN WERDEN!
    SpellIsNotUsableHere,             // 021: DER ZAUBER KANN HIER NICHT BENUTZT WERDEN!
    ReallyDropItem,                   // 022: SOLL DER GEGENSTAND WIRKLICH WEGGEWORFEN WERDEN?
    SameItemAlreadyInUse,             // 023: DER GLEICHE GEGENSTAND IST BEREITS IN GEBRAUCH.
    ReallyDropGold,                   // 024: SOLL DAS GOLD WIRKLICH ABGELEGT WERDEN?
    ReallyDropFood,                   // 025: SOLLEN DIE RATIONEN WIRKLICH ABGELEGT WERDEN?
    LockOpened,                       // 026: DU HAST DAS SCHLOß GEÖFFNET!
    HeardStrangeNoise,                // 027: DU HÖRST EIN SELTSAMES GERÄUSCH!
    ItemOpensDoor,                    // 028: DER GEGENSTAND ÖFFNET DIE TÜR!
    TrapDiscovered,                   // 029: DU ENTDECKST EINE FALLE!
    NoTrapDiscovered,                 // 030: DU ENTDECKST KEINE FALLE!
    TrapDisarmed,                     // 031: DU SCHAFFST ES, DIE FALLE ZU ENTSCHÄRFEN!
    LockpickOpensLock,                // 032: MIT HILFE DES DIETRICHS ÖFFNEST DU DAS SCHLOß!
    LockpickBreaks,                   // 033: DER DIETRICH BRICHT AB!
    LeaveGoldForGuild,                // 034: WILLST DU DAS GOLD, DAS HIER NOCH LIEGT, DER GILDE ÜBERLASSEN?
    HowMuchGoldToGive,                // 035: WIEVIEL GOLD ABGEBEN?
    NoMemberCanCarryThatMuchGold,     // 036: KEIN GRUPPENMITGLIED KANN SOVIEL GOLD TRAGEN!
    WrongAnswer,                      // 037: DAS WAR NICHT DIE RICHTIGE ANTWORT!
    WhatToSell,                       // 038: WAS WILLST DU MIR VERKAUFEN?
    WontBuyThat,                      // 039: DAS KAUFE ICH NICHT!
    OfferForItem,                     // 040: DAFÜR GEBE ICH DIR:
    HowManyToSell,                    // 041: WIEVIELE VERKAUFEN?
    Item,                             // 042: GEGENSTAND
    GiveItemsToMerchant,              // 043: WILLST DU SACHEN, DIE DIR GEHÖREN, DEM HÄNDLER SCHENKEN?
    GiveGoldToMerchant,               // 044: WILLST DU DAS GOLD, DAS HIER NOCH LIEGT, DEM HÄNDLER SCHENKEN?
    HorsesPrice,                      // 045: FÜR DIE PFERDE VERLANGE ICH:
    HorsesReady,                      // 046: DIE PFERDE STEHEN VOR DER STADT FÜR EUCH BEREIT!
    GiveGoldToHealer,                 // 047: WOLLT IHR DAS GOLD, DAS HIER NOCH LIEGT, DEM HEILER VERMACHEN?
    NotEnoughGoldForHealer,           // 048: IHR HABT NICHT GENUG GOLD, UM MEINE DIENSTE IN ANSPRUCH ZU NEHMEN!
    DestroyCursedItemsPrice,          // 049: ICH ZERSTÖRE DIE VERFLUCHTEN SACHEN FÜR:
    HealingPrice,                     // 050: FÜR DIE HEILUNG VERLANGE ICH:
    LockCannotBeOpened,               // 051: DAS SCHLOß LÄßT SICH NICHT ÖFFNEN!
    ExaminationPrice,                 // 052: FÜR DIE PRÜFUNG VERLANGE ICH:
    RaftPrice,                        // 053: FÜR EIN FLOß VERLANGE ICH:
    RaftReady,                        // 054: DAS FLOß LIEGT VOR DER STADT FÜR EUCH BEREIT!
    ShipPrice,                        // 055: FÜR EIN SCHIFF VERLANGE ICH:
    ShipReady,                        // 056: DAS SCHIFF LIEGT AM ANLEGER FÜR EUCH BEREIT!
    GiveGoldToInn,                    // 057: WOLLT IHR DAS GOLD, DAS NOCH HIER LIEGT, DEM GASTHAUS ÜBERLASSEN?
    InnRoomPrice,                     // 058: DAS MIETEN DES GÄSTEZIMMERS MIT VERPFLEGUNG KOSTET:
    TrapExplosion,                    // 059: IHR HÖRT EIN KLICKEN ... EXPLOSION ...
    TrapPoisonedArrows,               // 060: IHR HÖRT EIN LEISES KLICKEN ... VERGIFTETE PFEILE ...
    TrapPoisonGas,                    // 061: IHR HÖRT EIN LEISES ZISCHEN ... GRÜN SCHIMMERNDER NEBEL ...
    TrapBlindingFlash,                // 062: IHR HÖRT DAS GERÄUSCH EINER BRENNENDEN ZÜNDSCHNUR ... BLENDENDES LICHT ...
    TrapSleepGas,                     // 063: IHR HÖRT EIN RAUSCHEN ... BLÄULICHES GAS ...
    TrapBasiliskEye,                  // 064: VOR EUCH MATERIALISIERT SICH DAS AUGE EINES BASILISKEN!
    TrapSpores,                       // 065: IHR HÖRT GLAS ZERBRECHEN ... SELTSAME SPOREN ...
    NoticedTrapdoor,                  // 066: DU BEMERKST EINE FALLTÜR!
    NoticedFloorDisc,                 // 067: DU BEMERKST EINE RUNDE SCHEIBE, DIE IN DEN BODEN EINGELASSEN IST!
    StrangeFeelingAboutFloor,         // 068: DU HAST EIN SELTSAMES GEFÜHL, ALS DU DEN BODEN BETRACHTEST!
    NoticedStrongMagicRadiation,      // 069: DU BEMERKST EINE STARKE MAGISCHE STRAHLUNG!
    NoticedHiddenFloorSwitch,         // 070: DU BEMERKST EINEN VERSTECKTEN SCHALTER IM BODEN!
    GuildMembershipPrice,             // 071: GILDENBEITRITT KOSTET:
    LevelUpPrice,                     // 072: STUFENTRAINING KOSTET:
    WhatToBuy,                        // 073: WAS WILLST DU KAUFEN?
    HowManyToBuy,                     // 074: WIEVIELE KAUFEN?
    PriceForItem,                     // 075: DAFÜR VERLANGE ICH:
    NotEnoughGold,                    // 076: DU HAST NICHT GENUG GOLD!
    ItemAlreadyExamined,              // 077: DIESEN GEGENSTAND HABE ICH SCHON ÜBERPRÜFT!
    AttackMisses,                     // 078: TRIFFT NICHT!
    AttackParried,                    // 079: PARIERT DEN ANGRIFF!
    AttackHitsWithDamage,             // 080: TRIFFT MIT EINEM SCHADEN VON:
    AttackHitsButNoDamage,            // 081: TRIFFT, MACHT ABER KEINEN SCHADEN!
    AttackHitsButWeaponPowerless,     // 082: TRIFFT, ABER DIE WAFFE IST MACHTLOS!
    SpellCastSucceeds,                // 083: ZAUBERT, UND DER SPRUCH GELINGT!
    SpellCastFails,                   // 084: ZAUBERT, DOCH DER SPRUCH MISSLINGT!
    DefenderParries,                  // 085: PARIERT DEN ANGRIFF.
    DefenderFailsToParry,            // 086: SCHAFFT DIE PARADE NICHT!
    ShandraRescue,                    // 087: LANGSAM VERBLAßT DAS BLAUE GLÜHEN ... SHANDRA ...
    PartyDeath,                       // 088: WÄRME UMWALLT EURE KÖRPER ... GÖTTIN BALA ...
    ItemLabel,                        // 089: GEGENSTAND
    ShouldTakeItems,                  // 090: DU SOLLTEST FOLGENDE GEGENSTÄNDE MITNEHMEN:
    CastsSpell,                       // 091: ZAUBERT DEN SPRUCH:
    EachMemberReceives,               // 092: JEDES ANWESENDE GRUPPENMITGLIED ERHÄLT:
    AttackOrFlee,                     // 093: WILLST DU ANGREIFEN ODER FLIEHEN?
    FoodPrice,                        // 094: FÜR EINE RATION VERLANGE ICH:
    ExperienceNeededForNextLevel,     // 095: BRAUCHT FÜR DIE NÄCHSTE STUFE NOCH:
    WhichAbilitiesToTrain,            // 096: WELCHE FÄHIGKEITEN SOLLEN TRAINIERT WERDEN?
    HasLevel,                         // 097: HAT STUFE
    LevelReached,                     // 098: ERREICHT!
    MaxHPNow,                         // 099: LP MAXIMUM IST NUN:
    MaxSPNow,                         // 100: SP MAXIMUM IST NUN:
    SLPNow,                           // 101: SLP SIND NUN:
    SaveGame,                         // 102: SOLL DER AKTUELLE SPIELSTAND GESPEICHERT WERDEN?
    LoadGame,                         // 103: SOLL DER ALTE SPIELSTAND GELADEN WERDEN?
    ListenNothingHeard,               // 104: DU HÖRST NICHTS!
    ListenSomethingAt,                // 105: DU HÖRST ETWAS IN
    MetersAway,                       // 106: METERN ENTFERNUNG, IM
    North,                            // 107: NORDEN!
    NorthEast,                        // 108: NORDOSTEN!
    East,                             // 109: OSTEN!
    SouthEast,                        // 110: SÜDOSTEN!
    South,                            // 111: SÜDEN!
    SouthWest,                        // 112: SÜDWESTEN!
    West,                             // 113: WESTEN!
    NorthWest,                        // 114: NORDWESTEN!
    LearnFromWhichScrolll,            // 115: VON WELCHER SPRUCHROLLE MÖCHTEST DU LERNEN?
    NotASpellScroll,                  // 116: DIES IST KEINE SPRUCHROLLE!
    CannotLearnSpellOfThisClass,      // 117: DU KANNST EINEN SPRUCH DIESER KLASSE NICHT LERNEN!
    SpellLearnedScrollCrumbles,       // 118: DU SCHAFFST ES, DEN SPRUCH ZU LERNEN ... ZERFÄLLT ZU STAUB!
    SpellLearnFailedScrollCrumbles,   // 119: ES GELINGT DIR NICHT ... SPRUCHROLLE ZERFÄLLT ZU STAUB!
    PartyRestsEightHours,             // 120: DIE GRUPPE RUHT SICH ACHT STUNDEN AUS.
    NoFoodCannotRecover,              // 121: HAT KEINE RATION MEHR UND KANN SICH DESHALB NICHT ERHOLEN.
    Receives,                         // 122: ERHÄLT
    Back,                             // 123: ZURÜCK!
    DismissPartyMembers,              // 124: MÖCHTEST DU MITGLIEDER DER GRUPPE ENTLASSEN?
    CannotDismissDead,                // 125: DU KANNST TOTE NICHT ENTLASSEN!
    RefusesToLeave,                   // 126: WEIGERT SICH, DIE GRUPPE ZU VERLASSEN!
    CannotDismissPetrified,           // 127: DU KANNST VERSTEINERTE NICHT ENTLASSEN!
    NotEnoughExperienceForSpell,      // 128: DU HAST NICHT GENUG ERFAHRUNG, UM DEN SPRUCH ZU LERNEN!
    LeaderCannotBeDismissed,          // 129: DER ANFÜHRER KANN NICHT ENTLASSEN WERDEN!
    ConversationNotReturned,          // 130: DEIN INTERESSE AN EINER UNTERHALTUNG WIRD NICHT ERWIDERT!
    NotUnderstood,                    // 131: OFFENBAR VERSTEHT MAN DICH NICHT!
    ItemsLeftToPickUp,                // 132: HIER LIEGEN NOCH GEGENSTÄNDE, DIE DU MITNEHMEN SOLLTEST!
    NothingToSayAboutThat,            // 133: HMM, DAZU KANN ICH NICHTS SAGEN!
    ShowWhichItem,                    // 134: WELCHEN GEGENSTAND MÖCHTEST DU ZEIGEN?
    NotInterestedInItem,              // 135: DIESER GEGENSTAND INTERESSIERT MICH NICHT!
    GiveWhichItem,                    // 136: WELCHEN GEGENSTAND MÖCHTEST DU ABGEBEN?
    KeepYourGold,                     // 137: VIELEN DANK, ABER BEHALTE DEIN GOLD! ICH BRAUCHE ES NICHT.
    KeepYourFood,                     // 138: VIELEN DANK, ABER BEHALTE DEINE RATIONEN! ICH BRAUCHE SIE NICHT.
    NotInterestedInJoining,           // 139: ICH HABE KEIN INTERESSE DARAN, MIT EURER GRUPPE ZU ZIEHEN!
    PersonIsSleeping,                 // 140: STATT EINER ANTWORT HÖRST DU NUR EIN LAUTES SCHNARCHEN ...
    MapLegend,                        // 141: LEGENDE:
    MapLegendWall,                    // 142: = WAND
    MapLegendFloor,                   // 143: = BODEN
    MapLegendDoor,                    // 144: = TÜR
    MapLegendExit,                    // 145: = AUSGANG
    MapLegendTreasure,                // 146: = SCHATZ
    MapLegendRiddlemouth,             // 147: = RÄTSELMUND
    MapLegendMerchant,                // 148: = HÄNDLER
    MapLegendTeleporter,              // 149: = TELEPORTER
    MapLegendPosition,                // 150: = STANDORT
    HealerResurrect,                  // 151: DER HEILER LEGT EINE HAND AUF DEN KALTEN LEICHNAM.
    ResurrectedResponse,              // 152: ÖFFNET DIE AUGEN UND FRAGT: "WAS GIBT ES ZU ESSEN?"
    ChooseNewLeader,                  // 153: DIE GRUPPE HAT KEINEN ANFÜHRER MEHR, ES MUß EIN NEUER GEWÄHLT WERDEN.
    SpellFails,                       // 154: DER ZAUBERSPRUCH MIßLINGT.
    ApplyPotionToWhoseWeapon,         // 155: WESSEN WAFFE SOLL MIT DER FLÜSSIGKEIT BEHANDELT WERDEN?
    SpellDoesNotWorkHere,             // 156: DER SPRUCH FUNKTIONIERT HIER NICHT!
    WhoToHeal,                        // 157: WER SOLL GEHEILT WERDEN?
    WhoToResurrect,                   // 158: WER SOLL WIEDERBELEBT WERDEN?
    WhoseAshesToTransform,            // 159: WESSEN ASCHE WILLST DU WANDELN?
    WhoseDustToTransform,             // 160: WESSEN STAUB WILLST DU WANDELN?
    WhosePoisonToCure,                // 161: WESSEN VERGIFTUNG WILLST DU HEILEN?
    WhoseParalysisToCure,             // 162: WESSEN LÄHMUNG WILLST DU HEILEN?
    WhoseDiseaseToCure,               // 163: WESSEN KRANKHEIT WILLST DU HEILEN?
    WhoseAgingToCure,                 // 164: WESSEN ALTERUNG WILLST DU HEILEN?
    WhosePetrificationToCure,         // 165: WESSEN VERSTEINERUNG WILLST DU HEILEN?
    WhosePanicToRemove,               // 166: WESSEN PANIK BESEITIGEN?
    WhoseIrritationToRemove,          // 167: WESSEN IRRITATION BESEITIGEN?
    WhoseBlindnessToCure,             // 168: WESSEN BLINDHEIT WILLST DU HEILEN?
    WhoseMadnessToCure,               // 169: WESSEN VERRÜCKTHEIT WILLST DU HEILEN?
    WhichMonsterToParalyze,           // 170: WELCHES MONSTER WILLST DU LÄHMEN?
    WhichGroupToSleep,                // 171: WELCHE ROTTE SOLL IN SCHLAF VERSETZT WERDEN?
    WhichMonsterToPanic,              // 172: WELCHES MONSTER SOLL IN PANIK VERSETZT WERDEN?
    WhichMonsterToIrritate,           // 173: WELCHES MONSTER SOLL IRRITIERT WERDEN?
    WhichMonsterToBlind,              // 174: WELCHES MONSTER SOLL GEBLENDET WERDEN?
    WhichUndeadToDestroy,             // 175: WELCHER UNTOTE SOLL VERNICHTET WERDEN?
    WhichUndeadGroup,                 // 176: AUF WELCHE ROTTE VON UNTOTEN?
    WhichCurseToRemove,               // 177: WELCHER FLUCH SOLL AUFGEHOBEN WERDEN?
    WhichItem,                        // 178: WELCHEN GEGENSTAND?
    WhoToHaste,                       // 179: WER SOLL BESCHLEUNIGT WERDEN?
    WhoseCursedItemToDestroy,         // 180: BEI WEM SOLL EIN VERFLUCHTER GEGENSTAND VERNICHTET WERDEN?
    WhoseItemToExamine,               // 181: BEI WEM SOLL EIN GEGENSTAND GEPRÜFT WERDEN?
    WhichMonster,                     // 182: WELCHES MONSTER?
    WhichMonsterGroup,                // 183: WELCHE ROTTE?
    WhichMonsterToDissolve,           // 184: WELCHES MONSTER WILLST DU AUFLÖSEN?
    CrystalBallBlockedByMagic,        // 185: STARKE MAGIE TRÜBT DIE KRISTALLKUGEL ...
    WhichDemonToBanish,               // 186: WELCHER DÄMON SOLL GEBANNT WERDEN?
    WhoseSpellPointsToRestore,        // 187: WESSEN SP SOLLEN ERNEUERT WERDEN?
    ApplyBalmToWhat,                  // 188: BALSAM AUF WAS ANWENDEN?
    WhoToRejuvenate,                  // 189: WELCHES GRUPPENMITGLIED SOLL VERJÜNGT WERDEN?
    ReallyQuitProgram,                // 190: WOLLEN SIE DAS PROGRAMM WIRKLICH VERLASSEN?
    TalkingToYourself,                // 191: SELBSTGESPRÄCHE SIND EIN ZEICHEN VON VERRÜCKTHEIT!
    PleaseInsertDisk,                 // 192: LEGEN SIE BITTE DISK
    PleaseInsertDiskSuffix,           // 193: EIN
    RemoveWriteProtection,            // 194: ENTFERNEN SIE BITTE DEN SCHREIBSCHUTZ VON DER DISKETTE.
    DiskError,                        // 195: ES GAB EINEN DISKETTEN FEHLER. WOLLEN SIE ES NOCHEINMAL VERSUCHEN?
    InnWelcome,                       // 196: WILLKOMMEN! DARF ICH EUCH EINES UNSERER ZIMMER FÜR DIE NACHT ANBIETEN?
    ExaminerWelcome,                  // 197: SEID GEGRÜßT, ABENTEURER! SOLL ICH GEGENSTÄNDE FÜR EUCH UNTERSUCHEN?
    GuildWelcome,                     // 198: HERZLICH WILLKOMMEN IN UNSERER GILDE. WAS KANN ICH FÜR EUCH TUN?
    AfterBattleLoot,                  // 199: NACH DEM KAMPF SAMMELT IHR DIE SACHEN EURER GEGNER EIN.
    ChooseBattleFormation,            // 200: WELCHE FORMATION SOLLEN DIE GRUPPENMITGLIEDER FÜR DIE NÄCHSTEN KÄMPFE EINNEHMEN?
    AltarRequires13,                  // 201: ALS IHR DEN ALTAR BERÜHRT ... NUR WENN 13 SIND AM GLEICHEN ORT ...
    LastMessage = AltarRequires13
}
