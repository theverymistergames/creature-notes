using System;
using MisterGames.Blackboards.Tables;
using MisterGames.Scenario.Events;

[Serializable]
[BlackboardTable(typeof(EventReference))]
public sealed class BlackboardTableEventReference : BlackboardTable<EventReference> {}