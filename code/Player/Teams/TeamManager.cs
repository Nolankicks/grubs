﻿using Grubs.Utils;

namespace Grubs.Player;

/// <summary>
/// Manages a list of teams of grubs.
/// </summary>
[Category( "Setup" )]
public sealed partial class TeamManager : Entity
{
	/// <summary>
	/// The single instance of this manager.
	/// <remarks>All <see cref="Grubs.States.BaseGamemode"/>s should be including a <see cref="TeamManager"/>.</remarks>
	/// </summary>
	public static TeamManager Instance { get; private set; } = null!;

	/// <summary>
	/// The list of all teams in this manager.
	/// </summary>
	[Net]
	public IList<Team> Teams { get; private set; }

	/// <summary>
	/// The index to the current team who is doing their turn.
	/// </summary>
	[Net]
	private int CurrentTeamNumber { get; set; }

	/// <summary>
	/// The team who is doing their turn.
	/// </summary>
	public Team CurrentTeam => Teams[CurrentTeamNumber];

	public TeamManager()
	{
		Transmit = TransmitType.Always;
		Instance = this;
	}

	public override void Spawn()
	{
		base.Spawn();

		if ( Game.IsClient )
			return;

		Instance = this;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		Instance = this;
	}

	public override void Simulate( IClient cl )
	{
		foreach ( var team in Teams )
			team.Simulate( cl );
	}

	public override void FrameSimulate( IClient cl )
	{
		foreach ( var team in Teams )
			team.FrameSimulate( cl );
	}

	/// <summary>
	/// Adds a new team
	/// </summary>
	/// <param name="clients">The clients that are a part of this team.</param>
	public void AddTeam( List<IClient> clients )
	{
		Game.AssertServer();

		var team = new Team( clients, GameConfig.TeamNames[Teams.Count].ToString(), Teams.Count );
		Teams.Add( team );
	}

	/// <summary>
	/// Sets the team who is currently playing.
	/// </summary>
	/// <param name="teamIndex">The index of the team that is now playing.</param>
	public void SetActiveTeam( int teamIndex )
	{
		Game.AssertServer();

		CurrentTeamNumber = teamIndex;
		CurrentTeam.PickNextClient();
		CurrentTeam.PickNextGrub();
	}

	/// <summary>
	/// Cycles the team list.
	/// </summary>
	public void Cycle()
	{
		Game.AssertServer();

		if ( CurrentTeamNumber == Teams.Count - 1 )
			SetActiveTeam( 0 );
		else
			SetActiveTeam( CurrentTeamNumber + 1 );
	}
}
