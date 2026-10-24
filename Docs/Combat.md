# Amberstar combat

The combat code is not part of the released source code (COMBAT.ASM is only a stub). These notes were made by disassembling the compiled program (German AMBRSTAR.68K, text segment 0x1594a-0x1a000). Offsets refer to the character data (OFFSETS.ASM) unless stated otherwise.


## Files
- com_back.amb (subfile = combat background nr $5b942), mon_data.amb (subfile = monster group $5b94c),
  mon_gfx.amb (subfile per monster type = byte 62 of monster data), TACTIC.ICN, F_T_ANIM.ICN.
- Song 16 at start.

## Grid
- 6 columns x 5 rows, pointers at $5ba40 + (row*6+col)*4. Rows 0-2 monsters (row 2 = front), rows 3-4 party (row 3 = front).
- Monster group: 3 words monster types, 3 bytes column masks (6 bits, MSB first = col 0). Type 1 -> row 2, type 2 -> row 1, type 3 -> row 0.
- Party: position = Order[member-1] (0..11) -> row 3 + Order/6. Members with body conditions & $e4 (petrified, dead, ashes, dust) are not placed.
- Tactical grid on screen at (208,48), 16x16 cells.

## Participants
- Monster participant: 24 byte header (0: word 0, 2: long monster data, 6: word, 8: long gfx, ...),
  then copy of monster data: 24+ = bytes 34..54 (slot counts, 21), 0, 46 conditions word (58), 48 HP (134), 50 (138), 52 (144), 54 (146), 56 items (306.., 21*40).
  -> 46 = body conditions, 47 = mind conditions, 27 = right hand count, 176 = right hand item, 29 = left hand count, 256 = left hand item.
- Party participant: word $ff00, long char data, word 0, word member nr, long 0.

## Conditions (char data 58 = body byte, 59 = mind byte)
- body: bit0 stunned, bit1 poisoned, bit2 petrified, bit3 diseased, bit4 aging, bit5 dead, bit6 ashes, bit7 dust
- mind: bit0 irritated, bit1 mad, bit2 sleeping, bit3 panic, bit4 blind

## Buttons (combat CIL)
0 Flee, 1 -, 2 Ok (start round), 3 Move (PartyPositions icon), 4 slow (Play), 5 fast (Forward), 6 Attack (Sword), 7 Parry (Shield), 8 Magic.
- sleeping/mad/panic: all actions disabled. stunned: 0,3,6,7 disabled.
- move disabled if no free destination. attack disabled if damage+bonus <= 0 or no target in reach;
  ranged weapon (right hand item type 5) needs ammo (left hand item type 6 with same ammo type at item byte 15 = item byte 2 of weapon).
- flee only from back row (row 4). magic disabled if irritated or no spell classes.

## Party actions ($5b956, 6 x 8 bytes: action, x, y, extra)
0 parry, 1 move(x,y), 2 flee, 3 attack(x,y, extra=1 ranged), 4 magic, 5 sleep(no action), 6 stunned, 7 petrified.
Status icons per action: [3,6,5,2,4,0x11,0x0a,0x0c].
At round start (L16a66): move and magic are reset to parry, attack is kept if the target cell is still occupied.
Automatic actions: petrified -> 7, stunned -> 6, sleeping -> 5, panic -> flee in row 4 else move to row 4 (same column, else parry),
mad -> random (random/2 % 37): bit0 -> attack random target in reach; row 4 and bit2 clear -> flee; else move to random destination.

## Monster AI (L16f38 per monster, before the party actions are added)
- initiative = speed (attr 4, 78) + bonus (162). Action list sorted (L18280), executed highest first (check).
- $5bada set (party surprised the monsters?) -> monsters do nothing (5).
- stunned -> skip movement (magic/attack still checked? goes to L170fa).
- sleeping -> nothing. panic -> retreat.
- morale: lost% = 100 - alive_of_type*100/initial_of_type; if morale byte (65) <= lost% -> retreat.
- retreat: in row 0 -> flee (2); else move one row back (same column, else col-1, else col+1).
- advance: wanted row = 2 - byte 66 (unless $5ba06: no party in row 3?); move one row towards it.
- at wanted row: magic: not irritated, $5ba0e < 25; random%25 -> spell class at 256+i, spell nr at 281+i; Probe(byte 63 = spell cast chance) -> action 4.
- attack: ranged weapon with ammo -> attack random party member (L175c4); ranged without ammo -> retreat.
  melee only from row 2: party member in row 3 same column or diagonal -> attack; otherwise move sideways towards one; else parry.

## Attack (L177c4), a4/a0 attacker participant/data, a5/a1 defender
- hit: party: (attack 6 + bonus 176 + Spell_3_data (weapon power)) / 2 * 2, monster: attack*2; Probe_scaled (value * $5ba0a / 100). fail -> miss text.
- parry: only if the defender's action is parry: parry 7 + bonus 178 (+ Spell_2_data armour for party); Probe success -> parried.
- damage = rnd % (damage 31 + bonus 150 + (str 72 + bonus 156)/25) - rnd % (protection 30 + bonus 148 + magic armour 33 + (stamina 80 + bonus 164)/25). <= 0 -> no damage text.
- party defender: wakes up (mind bit2 cleared), Do_damage (HP 0 -> dead).
- Probe(v): success if v >= rnd%100 (v >= 100 always, v < 0 never).

## Round execution (L172f4..)
- action records (16 bytes): 0 side (1 monster, 0 party), 1 member nr, 2 col, 3 row, 4 action, 6 x / spell class, 8 y / spell nr, 10 extra (ranged), 12 data ptr.
- success factor $5ba0a per actor: 100, diseased /2, irritated *75/100, blind /4 (used by Probe_scaled for the hit).
- dispatch: 0 parry (nothing), 1 move, 2 flee, 3 attack, 4 magic, 5-7 nothing. Stunned/sleeping actors skip move/flee; attack also skipped when panicking; magic skipped when irritated/panic/sleeping.
- move: target cell must be free, text 4.
- flee: dex 76 + bonus 160 + luck 84 + bonus 168 vs rnd%150 (>=150 always). Success: removed from grid, monster exp still counted ($5ba08 += exp 198), party member gets mind bit7 (fled), text 5; fail text 6.
- attack: target cell empty -> text 8. attacks = attacks per round (67), party haste bit ($5b863, Hurry_bitlist) doubles once and is cleared.
  attacker magic weapon bonus (32) < defender byte 64 (magic needed) -> text 4 (can't hurt).
  miss text 0, parried text 1 (names swapped), no damage text 3, hit: "A <2> B <3> n <9>" (texts in $4d19e).
  monster defender: wakes, HP 48 -= dmg, dead -> 46 bit5. party defender: wakes, Do_damage.
  ranged: monster reloads ammo from inventory, party consumes ammo (weight, hands) and reloads, no ammo -> action cleared.
- after each action: dead monsters removed and stored ($5b99a list, exp += 198). Party petrified -> action 7, dead -> removed.
- all monsters gone -> victory. round counter $5ba0e++ (monsters stop casting after 25 rounds).
- end of round: moves reset to parry; if party front row empty: back row moves to the front (text 11) if more than one member or already flagged last round.

## Spells (L18936, table L18990 by class)
- caster party: SP -= cost (spell data byte 1), Probe(use magic 15 + bonus 194) else fail (items always succeed). monster: SP (participant 50) -= cost, same probe.
- white: heal1-5 1+rnd%5, 2+%8, 4+%16, 8+%32, 16+%64; heal all 4+%16 each; cure: 10 poison,11 stun,12 disease,13 aging,14 petrified,15 wake all,16 panic,17 irritation,18 blind,19 mad.
  20 stun, 22 panic, 23 irritate, 24 blind on monster: resist Probe(antimagic 86+170) -> text 12; monster byte 68 bit2 -> immune text 13. 21 sleep group. 25/26 destroy undead (68 bit0) -> dead unless resisted.
- grey: armour 4-6, weapon 7-9, anti-magic 10-12 -> Spell_2/3/4_data = 5/10/15, duration 24/96/192 (5 min units). haste 23 (one member bit), mass haste 24.
- black 1-20: damage table $313c4 (min,max,anim per spell), targets by spell data byte 3: bit4 all, bit3 row, else one. element = spell data byte 0 bits 6-7 -> bit via $313c6; monster byte 69: element bit -> immune (13), bit+4 -> double damage.
  monster casting on party: resist Probe(antimagic + Spell_4_data). 21 dissolve: kill unless resisted. 22 magic arrows: 8 damage each unless resisted.
- special (monsters, items): 1-7,9,10 condition on random party member (resist incl. anti-magic spell): stun, poison, petrify, disease, aging, irritate, mad, panic, blind (texts 15-24). 8 sleep all party (text 22).
  13 banish demon (68 bit1): code kills the demon when its antimagic probe SUCCEEDS (looks reversed). 14 SP +1..25, 15 SP max, 17 youth -1..10 years (not below 15).

## End of combat (19662)
- conditions irritated, sleeping, panic, fled are cleared (mask $ff72 on the condition word).
- group removed from map (CD bit (map-1)*24 + group index + 1).
- loot: gold sum of monster gold (52), food (54), items of all killed monsters; exp = $5ba08 / members not dead/fled, text 14 "n EP.".

## Start (PERSONS.ASM CombatReq, released source)
- Yes: invisible -> Party_advantage (monsters do nothing in the first round). background = first set bit 16..29 of the tile flags - 15 (default 1).
- No: invisible -> escape. else Probe dex+luck vs rnd%150; fail -> Monster_advantage (first round runs immediately, party can't act).

## Data
- Monster data: 62 battle graphic (MON_GFX.AMB subfile), 63 spell cast chance, 64 magic weapon bonus needed to hurt it,
  65 morale, 66 wanted distance from the front row, 67 attacks per round, 68 flags (bit0 undead, bit1 demon,
  bit2 immune to the white condition spells), 69 elements (bits 0-3 immune, bits 4-7 vulnerable), 198 experience,
  256 25 spell classes, 281 25 spell numbers.
- Texts: message file 4 (Message 78-103) and the 9th message file (Message 202-226) which follows message file 8.
  Message 87 (Shandra rescues the party once) and 88 (the party is dead) belong to the end of a lost battle.
- COM_BACK.AMB: 14 backgrounds of 176x112 pixels (4 bitplanes). MON_GFX.AMB: 21 monster graphics with a frame table.
- Damage table of the black spells in the program data: 6 bytes (min, max, animation words) per spell after an unused entry whose last 4 bytes are the immunity bit of each element ([0, 2, 1, 3]: fire, water, earth, wind).

## Targets and order
- party move (L162ac): one of the 8 neighbor cells, rows 3-4 only, the cell must be free and not the move target of another member.
- melee targets (L1634c): from the cell in front (row - 1 = 2) or diagonal in front, i.e. only from row 3 against row 2.
  ranged targets: every monster.
- action list (L18280): stable bubble sort by initiative (speed + bonus) ascending, executed from the end (fastest first).
  The list holds the monsters in grid order first, then the party members.

## Party death (0x6fe Update_status_block, 0x7a2)
- If no member can be the active member (conditions & $e586: dead, ashes, dust, petrified, stunned, fled, sleeping, mad),
  a battle ends. If the active member fled or any member fled, the party fled ($5be90).
- Otherwise: if quest bit 6 is set and Probe(average luck + bonus of the members) succeeds, the "Resurrection sequence"
  (0x9a8) runs: song 19, next day 7:20, map 70 at 16,13 facing north, all conditions cleared, HP filled, Message 87 (Shandra).
- Else the "Game Over sequence" (0xba4): song 19, Message 88, then the game restarts.
