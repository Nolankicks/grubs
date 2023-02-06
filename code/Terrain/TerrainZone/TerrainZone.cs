﻿using Grubs.Terrain.Shapes;

namespace Grubs.Terrain;

/// <summary>
/// Handles a zone in the terrain that can trigger on entities.
/// </summary>
[Category( "Terrain" )]
public partial class TerrainZone : Entity
{
	/// <summary>
	/// Networked list of all damage zones in the terrain.
	/// </summary>
	public new static IList<TerrainZone> All { get; private set; } = new List<TerrainZone>();

	/// <summary>
	/// The shape that this zone is taking.
	/// </summary>
	[Net]
	protected ZoneShape Shape { get; set; } = BoxShape.WithSize( Vector3.One );

	/// <summary>
	/// The amount of turns until the zone is removed.
	/// </summary>
	public int ExpireAfterTurns
	{
		get => _expireAfterTurns;
		set
		{
			_expireAfterTurns = value;
			if ( _expireAfterTurns == 0 )
				QueueToRemove.Enqueue( this );
		}
	}
	[Net]
	private int _expireAfterTurns { get; set; } = -1;

	private static readonly Queue<TerrainZone> QueueToAdd = new();
	private static readonly Queue<TerrainZone> QueueToRemove = new();

	public override void Spawn()
	{
		base.Spawn();

		if ( Game.IsClient )
			return;

		All.Add( this );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		All.Add( this );
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( All.Contains( this ) )
			QueueToRemove.Enqueue( this );
	}

	/// <summary>
	/// Sets the position this zone sits at.
	/// </summary>
	/// <param name="position">The position for the zone to sit at.</param>
	/// <returns>The terrain zone instance.</returns>
	public TerrainZone WithPosition( Vector3 position )
	{
		Position = position;
		return this;
	}

	/// <summary>
	/// Sets the shape that the zone will use.
	/// </summary>
	/// <param name="shape">The shape to use.</param>
	/// <returns>The terrain zone instance.</returns>
	public TerrainZone WithShape( ZoneShape shape )
	{
		Shape = shape;
		return this;
	}

	/// <summary>
	/// Sets the amount of turns it will take before this zone expires.
	/// <remarks><see ref="turns"/> beings less than zero (0) will result in the zone never expiring.</remarks>
	/// </summary>
	/// <param name="turns">The amount of turns until the zone expires.</param>
	/// <returns>The terrain zone instance.</returns>
	public TerrainZone ExpireAfter( int turns )
	{
		ExpireAfterTurns = turns;
		return this;
	}

	/// <summary>
	/// Queues the zone to be added to the zone list.
	/// </summary>
	/// <returns>The terrain zone as the provided type.</returns>
	public virtual T Finish<T>() where T : TerrainZone
	{
		Game.AssertServer();

		Shape.Finish( this );
		QueueToAdd.Enqueue( this );

		return (this as T)!;
	}

	/// <summary>
	/// Queues the zone to be added to the zone list.
	/// </summary>
	/// <returns>The terrain zone in the base type.</returns>
	public TerrainZone Finish()
	{
		return Finish<TerrainZone>();
	}

	/// <summary>
	/// Returns whether or not an entity is inside this zone.
	/// </summary>
	/// <param name="entity">The entity position to check.</param>
	/// <returns>Whether or not the entity is inside this zone.</returns>
	public virtual bool InZone( Entity entity ) => Shape.InZone( entity );

	/// <summary>
	/// Called when the zone needs to act on an entity inside of it.
	/// </summary>
	/// <param name="entity">The entity that is inside the zone.</param>
	public virtual void Trigger( Entity entity )
	{
		Game.AssertServer();
	}

	/// <summary>
	/// Debug console variable to see the zones area.
	/// </summary>
	[ConVar.Server( "zone_debug" )]
	public static bool ZoneDebug { get; set; }

	/// <summary>
	/// Adds/Removes queued zones and shows all the zones if <see cref="ZoneDebug"/> is true.
	/// </summary>
	[Event.Tick.Server]
	public static void Tick()
	{
		while ( QueueToAdd.TryDequeue( out var zone ) )
			All.Add( zone );

		while ( QueueToRemove.TryDequeue( out var zone ) )
		{
			All.Remove( zone );
			zone.Delete();
		}

		if ( !ZoneDebug )
			return;

		foreach ( var zone in All )
			zone.Shape.DebugDraw();
	}
}
