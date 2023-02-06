﻿using Grubs.Weapons.Base;

namespace Grubs.Player;

public sealed partial class GrubsInventory : EntityComponent
{
	[Net]
	public GrubWeapon? LastUsedWeapon { get; set; }

	[Net]
	public IList<GrubWeapon> Items { get; private set; } = null!;

	public void Add( GrubWeapon weapon, bool makeActive = false )
	{
		if ( !weapon.IsValid() )
			return;

		// Handle picking up a weapon we already have.
		if ( IsCarrying( weapon ) )
		{
			var existingWeapon = Items.FirstOrDefault( item => item.GetType() == weapon.GetType() );
			// -1 represents unlimited ammo, so don't add ammo in this case.
			if ( existingWeapon is not null && existingWeapon.Ammo != -1 )
				existingWeapon.Ammo++;

			weapon.Delete();
			return;
		}

		// Handle picking up a weapon we do not have.
		Items.Add( weapon );
		weapon.Parent = Entity;
		weapon.OnCarryStart( Entity );
	}

	public bool IsCarrying( GrubWeapon weapon )
	{
		return Items.Any( item => item.Name == weapon.Name );
	}

	public bool HasAmmo( int index )
	{
		return Items[index].Ammo != 0;
	}

	[ConCmd.Server]
	public static void EquipItemByIndex( int index )
	{
		var team = ConsoleSystem.Caller.Pawn as Team;
		if ( ConsoleSystem.Caller.IsListenServerHost && TeamManager.Instance.CurrentTeam.ActiveClient.IsBot )
			team = TeamManager.Instance.CurrentTeam;

		if ( team is null )
			return;

		var grub = team.ActiveGrub;
		if ( !grub.IsTurn )
			return;

		var inventory = team.Inventory;
		var weapon = inventory.Items[index];
		inventory.LastUsedWeapon = weapon;
		grub.EquipWeapon( weapon );
	}
}
