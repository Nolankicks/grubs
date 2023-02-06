﻿using Grubs.Player;
using Grubs.States;

namespace Grubs.Weapons.Base;

/// <summary>
/// A weapon the grubs can use.
/// </summary>
[Category( "Weapons" )]
public abstract partial class GrubWeapon : BaseCarriable, IResolvable
{
	public bool Resolved => !IsFiring && !IsCharging;

	/// <summary>
	/// The name of the weapon.
	/// </summary>
	public virtual string WeaponName => AssetDefinition.WeaponName;

	/// <summary>
	/// The path to the weapon model.
	/// </summary>
	public virtual string Icon => AssetDefinition.Icon;

	/// <summary>
	/// The path to the weapon model.
	/// </summary>
	protected virtual string ModelPath => AssetDefinition.Model;

	/// <summary>
	/// The way that this weapon fires.
	/// </summary>
	protected virtual FiringType FiringType => AssetDefinition.FiringType;

	/// <summary>
	/// The way that this weapon is held by the grub.
	/// </summary>
	protected virtual HoldPose HoldPose => AssetDefinition.HoldPose;

	/// <summary>
	/// Whether or not this weapon should have an aim reticle.
	/// </summary>
	public virtual bool HasReticle => AssetDefinition.HasReticle;

	/// <summary>
	/// Whether or not this weapon should have an aim reticle.
	/// </summary>
	public virtual int Uses => AssetDefinition.Uses;

	/// <summary>
	/// The amount of times this gun can be used before being removed.
	/// </summary>
	[Net, Predicted, Local]
	public int Ammo { get; set; }

	/// <summary>
	/// The current charge the weapon has.
	/// </summary>
	[Net, Predicted]
	protected int Charge { get; private set; }

	/// <summary>
	/// Whether or not the weapon is currently being charged.
	/// </summary>
	[Net, Predicted]
	public bool IsCharging { get; private set; }

	/// <summary>
	/// Whether or not the weapon is currently being fired.
	/// </summary>
	[Net, Predicted]
	public bool IsFiring { get; protected set; }

	/// <summary>
	/// The time since the last attack started.
	/// </summary>
	[Net, Predicted]
	public TimeSince TimeSinceFire { get; private set; }

	/// <summary>
	/// The amount of times this weapon has been used this turn.
	/// </summary>
	[Net, Predicted]
	public int CurrentUses { get; protected set; }

	/// <summary>
	/// Whether or not this weapon has a special hat associated with it.
	/// </summary>
	[Net]
	private bool WeaponHasHat { get; set; }

	/// <summary>
	/// The asset definition this weapon is implementing.
	/// </summary>
	// TODO: This is cancer https://github.com/Facepunch/sbox-issues/issues/2282
	public WeaponAsset AssetDefinition
	{
		get => _assetDefinition;
		private init
		{
			_assetDefinition = value;

			Name = value.WeaponName;
			SetModel( value.Model );
			Ammo = value.InfiniteAmmo ? -1 : 0;
			WeaponHasHat = CheckWeaponForHat();
		}
	}
	[Net]
	private WeaponAsset _assetDefinition { get; set; } = null!;

	/// <summary>
	/// Helper property to grab the Grub that is holding this weapon.
	/// </summary>
	protected Grub Holder => (Parent as Grub)!;

	private const int MaxCharge = 100;

	public GrubWeapon()
	{
	}

	public GrubWeapon( WeaponAsset assetDefinition )
	{
		AssetDefinition = assetDefinition;
	}

	private bool CheckWeaponForHat()
	{
		for ( var i = 0; i < BoneCount; i++ )
		{
			if ( GetBoneName( i ) == "head" )
				return true;
		}

		return false;
	}

	public override void ActiveStart( Entity ent )
	{
		if ( ent is not Grub grub )
			return;

		EnableDrawing = true;
		grub.SetAnimParameter( "holdpose", (int)HoldPose );
		SetParent( grub, true );

		base.OnActive();
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		if ( ent is not Grub grub )
			return;

		if ( CurrentUses > 0 )
			TakeAmmo();

		EnableDrawing = false;
		ShowWeapon( grub, false );
		SetParent( Owner );

		CurrentUses = 0;
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		if ( !BaseGamemode.Instance!.MovementOnly && (!IsFiring || Uses > 1) )
			CheckFireInput();
	}

	private void CheckFireInput()
	{
		// Only fire if our grub is grounded and we haven't used our turn.
		var controller = (Owner as Team)!.ActiveGrub.Controller;
		/*if ( !controller.IsGrounded || BaseGamemode.Instance!.UsedTurn )
			return;*/

		switch ( FiringType )
		{
			case FiringType.Charged:
				if ( Input.Down( InputButton.PrimaryAttack ) )
				{
					IsCharging = true;
					Charge++;
					Charge = Charge.Clamp( 0, MaxCharge );
					if ( Charge != MaxCharge )
						break;

					IsCharging = false;
					Fire();
					Charge = 0;
				}

				if ( Input.Released( InputButton.PrimaryAttack ) )
				{
					IsCharging = false;
					Fire();
					Charge = 0;
				}

				break;
			case FiringType.Instant:
				if ( Input.Pressed( InputButton.PrimaryAttack ) )
					Fire();

				break;
			default:
				Log.Error( $"Got invalid firing type: {FiringType}" );
				break;
		}
	}

	/// <summary>
	/// Called when the weapon has been fired.
	/// </summary>
	protected virtual void Fire()
	{
		IsFiring = true;
		TimeSinceFire = 0;
		CurrentUses++;

		var continueFiring = OnFire();
		if ( continueFiring )
			return;

		IsFiring = false;
		OnFireFinish();
	}

	/// <summary>
	/// Called to do your main firing logic.
	/// </summary>
	/// <returns>Whether or not the weapon is going to continue firing.</returns>
	protected virtual bool OnFire()
	{
		if ( Owner is not Grub grub )
			return false;

		grub.SetAnimParameter( "fire", true );
		PlaySound( AssetDefinition.FireSound );

		return false;
	}

	/// <summary>
	/// Called when firing has finished.
	/// </summary>
	protected virtual void OnFireFinish()
	{
		if ( CurrentUses >= Uses )
			BaseGamemode.Instance!.UseTurn( true );
	}

	/// <summary>
	/// Takes ammo from the weapon.
	/// </summary>
	public void TakeAmmo()
	{
		Ammo--;
	}

	/// <summary>
	/// Sets whether the weapon should be visible.
	/// </summary>
	/// <param name="grub">The grub to update weapon visibility on.</param>
	/// <param name="show">Whether or not the weapon should be shown.</param>
	public void ShowWeapon( Grub grub, bool show )
	{
		if ( show )
		{
			PlaySound( AssetDefinition.DeploySound );
		}
		EnableDrawing = show;
		grub.SetAnimParameter( "holdpose", show ? (int)HoldPose : (int)HoldPose.None );

		if ( WeaponHasHat )
			grub.SetHatVisible( !show );
	}
}
