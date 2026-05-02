using System.Collections.Generic;
using UnityEngine;

public static class SpellResolver
{
    public static void Resolve(SpellData spell, TacticalCharacter caster, List<Cell> affectedCells)
    {
        int totalDmg = 0, totalHeal = 0;
        var secondary = new List<string>();

        foreach (SpellEffect effect in spell.effects)
            ApplyEffect(effect, spell, caster, affectedCells, ref totalDmg, ref totalHeal, secondary);

        // ---- Ligne principale : "Joueur lance Sort [-X] [+Y]" ----
        string cn = Fmt(caster);
        string sn = $"<b>{spell.spellName}</b>";
        string log;
        if      (totalDmg > 0 && totalHeal > 0)
            log = $"{cn} lance {sn} <color=#FF6B6B>-{totalDmg}</color> <color=#88FF88>+{totalHeal}</color>";
        else if (totalDmg > 0)
            log = $"{cn} lance {sn} <color=#FF6B6B>-{totalDmg}</color>";
        else if (totalHeal > 0)
            log = $"{cn} lance {sn} <color=#88FF88>+{totalHeal}</color>";
        else
            log = $"{cn} lance {sn}";

        CombatLog.Append(log);

        // ---- Effets secondaires (statuts, ressources, déplacements…) ----
        foreach (var msg in secondary)
            CombatLog.Append(msg);

        caster.NotifySpellCast(spell);
    }

    // =========================================================
    // APPLICATION D'UN EFFET
    // =========================================================
    private static void ApplyEffect(
        SpellEffect effect, SpellData spell, TacticalCharacter caster,
        List<Cell> cells, ref int totalDmg, ref int totalHeal, List<string> sec)
    {
        foreach (Cell cell in cells)
        {
            TacticalCharacter target = GetCharacterAt(cell);

            switch (effect.type)
            {
                // ---- Dégâts ----
                case SpellEffectType.Damage:
                    if (target == null) break;
                    int dmg = effect.value;
                    if (effect.condition == SpellCondition.TargetHPBelow && target.CurrentHP < effect.conditionThreshold)
                        dmg = Mathf.RoundToInt(dmg * effect.conditionMultiplier);
                    else if (effect.condition == SpellCondition.FromBehind && IsFromBehind(caster, target))
                        dmg = Mathf.RoundToInt(dmg * effect.conditionMultiplier);

                    var pm = caster.GetComponent<PassiveManager>();
                    if (pm != null)
                    {
                        int dist = Mathf.Abs(caster.CurrentCell.GridX - cell.GridX)
                                 + Mathf.Abs(caster.CurrentCell.GridY - cell.GridY);
                        dmg = pm.ModifyOutgoingDamage(dmg, spell, target, dist);
                    }
                    target.TakeDamage(dmg, caster);
                    totalDmg += dmg;
                    break;

                case SpellEffectType.SelfDamage:
                    caster.TakeDamage(effect.value, null);
                    break;

                // ---- Soin ----
                case SpellEffectType.Heal:
                    (target ?? caster).Heal(effect.value);
                    totalHeal += effect.value;
                    break;

                // ---- Saignement ----
                case SpellEffectType.Bleed:
                    if (target == null) break;
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.Bleed, effect.value, effect.duration, true));
                    sec.Add($"{Fmt(target)} : <color=#FF4444>Saignement</color> (<color=#FF6B6B>-{effect.value}</color>/tour, {effect.duration}T)");
                    break;

                // ---- Statuts offensifs ----
                case SpellEffectType.Silence:
                    if (target == null) break;
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.Silence, 0, effect.duration, true));
                    sec.Add($"{Fmt(target)} : <color=#BB88FF>Silence</color> ({effect.duration}T)");
                    break;

                case SpellEffectType.GravityDebuff:
                    if (target == null) break;
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.GravityDebuff, 0, effect.duration, true));
                    sec.Add($"{Fmt(target)} : <color=#BB88FF>Gravité</color> ({effect.duration}T)");
                    break;

                case SpellEffectType.ReduceFirstAttack:
                    if (target == null) break;
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.ReducedAttack, effect.value, effect.duration, true));
                    sec.Add($"{Fmt(target)} : Frappe réduite ({effect.duration}T)");
                    break;

                case SpellEffectType.RemovePM:
                    if (target == null) break;
                    { int removed = target.RemovePM(effect.value);
                      if (removed > 0) sec.Add($"{Fmt(target)} : <color=#88AAFF>-{removed} PM</color>"); }
                    break;

                case SpellEffectType.StealPM:
                    if (target == null) break;
                    { int stolen = target.RemovePM(effect.value);
                      caster.AddBonusPM(stolen);
                      if (stolen > 0) sec.Add($"{Fmt(caster)} vole <color=#88AAFF>{stolen} PM</color> à {Fmt(target)}"); }
                    break;

                // ---- Statuts défensifs (sur le caster) ----
                case SpellEffectType.Shield:
                    caster.AddStatusEffect(new StatusEffect(StatusEffectType.Shield, effect.value, effect.duration));
                    sec.Add($"{Fmt(caster)} : <color=#4499FF>Bouclier +{effect.value} PV</color>");
                    break;

                case SpellEffectType.DamageReduction:
                    caster.AddStatusEffect(new StatusEffect(StatusEffectType.DamageReduction, effect.value, effect.duration));
                    sec.Add($"{Fmt(caster)} : Réduction <color=#4499FF>-{effect.value} dmg</color> ({effect.duration}T)");
                    break;

                case SpellEffectType.Thorns:
                    caster.AddStatusEffect(new StatusEffect(StatusEffectType.Thorns, effect.value, effect.duration));
                    sec.Add($"{Fmt(caster)} : <color=#88FF44>Épines</color> ({effect.value} dmg, {effect.duration}T)");
                    break;

                case SpellEffectType.Invisible:
                    caster.AddStatusEffect(new StatusEffect(StatusEffectType.Invisible, 0, effect.duration));
                    sec.Add($"{Fmt(caster)} : Invisible ({effect.duration}T)");
                    break;

                case SpellEffectType.LastBreath:
                    caster.AddStatusEffect(new StatusEffect(StatusEffectType.LastBreath, 1, effect.duration));
                    sec.Add($"{Fmt(caster)} : <color=#FFFF44>Second Souffle</color> actif");
                    break;

                // ---- Ressources ----
                case SpellEffectType.BonusPA:
                    caster.AddBonusPA(effect.value);
                    sec.Add($"{Fmt(caster)} : <color=#FFD700>+{effect.value} PA</color>");
                    break;

                case SpellEffectType.BonusPM:
                    caster.AddBonusPM(effect.value);
                    sec.Add($"{Fmt(caster)} : <color=#88FF88>+{effect.value} PM</color>");
                    break;

                case SpellEffectType.BonusPANextTurn:
                    caster.AddNextTurnBonusPA(effect.value);
                    sec.Add($"{Fmt(caster)} : <color=#FFD700>+{effect.value} PA</color> (prochain tour)");
                    break;

                case SpellEffectType.BonusRange:
                    caster.AddBonusRange(effect.value, effect.duration);
                    sec.Add($"{Fmt(caster)} : <color=#88CCFF>+{effect.value} portée</color> ({effect.duration}T)");
                    break;

                case SpellEffectType.ConvertPMtoPA:
                    if (caster.RemovePM(1) > 0)
                    {
                        caster.AddBonusPA(1);
                        sec.Add($"{Fmt(caster)} : 1 PM → 1 PA");
                    }
                    break;

                // ---- Nettoyage ----
                case SpellEffectType.Cleanse:
                    { var t = target ?? caster;
                      t.ClearAllDebuffs();
                      sec.Add($"{Fmt(t)} est purifié"); }
                    break;

                // ---- Déplacements ----
                case SpellEffectType.Push:
                    if (target == null) break;
                    Push(target, caster.CurrentCell, effect.value);
                    sec.Add($"{Fmt(target)} repoussé");
                    break;

                case SpellEffectType.Pull:
                    PullToward(caster, cell, effect.value);
                    sec.Add($"{Fmt(caster)} s'approche");
                    break;

                case SpellEffectType.Swap:
                    if (target == null) break;
                    Swap(caster, target);
                    sec.Add($"{Fmt(caster)} ↔ {Fmt(target)}");
                    break;

                case SpellEffectType.Teleport:
                    if (!cell.IsOccupied && cell.IsWalkable)
                    {
                        MoveInstant(caster, cell);
                        sec.Add($"{Fmt(caster)} se téléporte");
                    }
                    break;

                // ---- Mur temporaire ----
                case SpellEffectType.CreateWall:
                    if (cell != null && !cell.IsOccupied)
                    {
                        cell.IsWalkable = false;
                        sec.Add("Obstacle créé");
                    }
                    break;
            }
        }
    }

    // =========================================================
    // UTILITAIRES
    // =========================================================
    private static string Fmt(TacticalCharacter t) => t != null ? $"<b>{t.name}</b>" : "?";

    private static TacticalCharacter GetCharacterAt(Cell cell)
    {
        if (cell == null || !cell.IsOccupied) return null;
        return cell.Occupant?.GetComponent<TacticalCharacter>();
    }

    private static bool IsFromBehind(TacticalCharacter attacker, TacticalCharacter target)
    {
        Cell a = attacker.CurrentCell;
        Cell t = target.CurrentCell;
        int dx = a.GridX - t.GridX;
        int dy = a.GridY - t.GridY;
        switch (target.Facing)
        {
            case FacingDirection.SouthEast: return dx < 0 && dy > 0;
            case FacingDirection.SouthWest: return dx > 0 && dy > 0;
            case FacingDirection.NorthEast: return dx < 0 && dy < 0;
            case FacingDirection.NorthWest: return dx > 0 && dy < 0;
            default: return false;
        }
    }

    private static void Push(TacticalCharacter target, Cell pushSource, int distance)
    {
        Cell tc = target.CurrentCell;
        int stepX = tc.GridX - pushSource.GridX;
        int stepY = tc.GridY - pushSource.GridY;
        if (stepX != 0) stepX = stepX > 0 ? 1 : -1;
        if (stepY != 0) stepY = stepY > 0 ? 1 : -1;

        Cell dest = tc;
        for (int i = 0; i < distance; i++)
        {
            Cell next = GridManager.Instance.GetCell(dest.GridX + stepX, dest.GridY + stepY);
            if (next == null || !next.IsWalkable || next.IsOccupied) break;
            dest = next;
        }
        if (dest != tc) MoveInstant(target, dest);
    }

    private static void PullToward(TacticalCharacter caster, Cell targetCell, int distance)
    {
        Cell origin = caster.CurrentCell;
        int stepX = targetCell.GridX - origin.GridX;
        int stepY = targetCell.GridY - origin.GridY;
        if (stepX != 0) stepX = stepX > 0 ? 1 : -1;
        if (stepY != 0) stepY = stepY > 0 ? 1 : -1;

        Cell dest = GridManager.Instance.GetCell(targetCell.GridX - stepX, targetCell.GridY - stepY);
        if (dest == null || !dest.IsWalkable || dest.IsOccupied) return;
        MoveInstant(caster, dest);
    }

    private static void Swap(TacticalCharacter a, TacticalCharacter b)
    {
        Cell ca = a.CurrentCell;
        Cell cb = b.CurrentCell;
        ca.ClearOccupant();
        cb.ClearOccupant();
        ca.SetOccupant(b.gameObject);
        cb.SetOccupant(a.gameObject);
        if (GridManager.Instance != null)
        {
            b.transform.position = GridManager.Instance.GridToWorldFace(ca.GridX, ca.GridY);
            a.transform.position = GridManager.Instance.GridToWorldFace(cb.GridX, cb.GridY);
        }
        else
        {
            b.transform.position = ca.WorldPosition;
            a.transform.position = cb.WorldPosition;
        }
        b.ForceSetCell(ca);
        a.ForceSetCell(cb);
    }

    private static void MoveInstant(TacticalCharacter character, Cell destination)
    {
        character.CurrentCell?.ClearOccupant();
        destination.SetOccupant(character.gameObject);
        character.transform.position = GridManager.Instance != null
            ? GridManager.Instance.GridToWorldFace(destination.GridX, destination.GridY)
            : destination.WorldPosition;
        character.ForceSetCell(destination);
    }
}
