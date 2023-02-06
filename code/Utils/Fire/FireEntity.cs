﻿using Grubs.Player;
using Grubs.States;
using Grubs.Terrain;
using Grubs.Utils.Extensions;

namespace Grubs.Utils;

/// <summary>
/// A utility class to use fire.
/// </summary>
[Category( "Weapons" )]
public sealed class FireEntity : ModelEntity, IResolvable
{
	public bool Resolved => Time.Now > _expiryTime;

	private TimeSince TimeSinceLastTick { get; set; }
	private Vector3 MoveDirection { get; set; }
	private Vector3 _desiredPosition;

	private readonly float _expiryTime;
	private const float FireTickRate = 0.15f;

	public FireEntity()
	{
	}

	public FireEntity( Vector3 startPos, Vector3 movementDirection )
	{
		Position = startPos + new Vector3().WithX( Game.Random.Int( 30 ) );
		_desiredPosition = Position;

		var tr = Trace.Ray( startPos, startPos + movementDirection ).Run();

		if ( tr.Hit )
			MoveDirection = Vector3.Reflect( MoveDirection, tr.Normal );
		else
			MoveDirection = -movementDirection / 2f;

		_expiryTime = Time.Now + 3f;
		TimeSinceLastTick = Game.Random.Float( 0.25f );
	}

	public override void Spawn()
	{
		base.Spawn();
		SetModel( "particles/flamemodel.vmdl" );
	}

	[Sandbox.Event.Tick.Server]
	private void Tick()
	{
		Position = Vector3.Lerp( Position, _desiredPosition, Time.Delta * 10f );
		if ( Time.Now > _expiryTime )
			Delete();

		if ( TimeSinceLastTick < FireTickRate )
			return;

		Move();
		TimeSinceLastTick = 0f;
	}

	private void Move()
	{
		const float fireSize = 20f;

		var midpoint = new Vector3( _desiredPosition.x, _desiredPosition.z );

		BaseGamemode.Instance!.TerrainMap.EditCircle( midpoint, fireSize, TerrainModifyMode.Remove );

		var sourcePos = _desiredPosition;
		foreach ( var grub in All.OfType<Grub>().Where( x => Vector3.DistanceBetween( sourcePos, x.Position ) <= fireSize ) )
		{
			if ( !grub.IsValid() || grub.LifeState != LifeState.Alive )
				continue;

			var dist = Vector3.DistanceBetween( _desiredPosition, grub.Position );
			if ( dist > fireSize )
				continue;

			var distanceFactor = 1.0f - Math.Clamp( dist / fireSize, 0, 1 );
			var force = distanceFactor * 1000; // TODO: PhysicsGroup/Body is invalid on grubs

			var dir = (grub.Position - _desiredPosition).Normal;
			grub.ApplyAbsoluteImpulse( dir * force );

			grub.TakeDamage( DamageInfoExtension.FromExplosion( 6, _desiredPosition, Vector3.Up * 32, this ) );
		}

		_desiredPosition += MoveDirection * 1.5f;
		var hitresult = Trace.Sphere( fireSize * 1.5f, Position, _desiredPosition ).Run();
		var grounded = hitresult.Hit;

		if ( grounded )
		{
			MoveDirection += Vector3.Random.WithY( 0 ) * 2.5f;
			MoveDirection += hitresult.Normal * 0.5f;
			MoveDirection = MoveDirection.Normal * 5f;
		}
		else
		{
			MoveDirection += Vector3.Down * 2.5f;
			MoveDirection = MoveDirection.Normal * 10f;
		}
	}
}
