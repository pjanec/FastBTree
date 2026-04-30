using System;
using Fbt.Examples.FluentBTree;

var interpreter = AmbushTree.CreateInterpreter();
var bb = new CombatBlackboard { AmmoCount = 5, ThreatVisible = true, EngagementRange = 50f };
var state = new Fbt.BehaviorTreeState();
var ctx = new CombatContext { DeltaTime = 0.016f };

Console.WriteLine("Ambush_BT demo (5 ticks):");
for (int i = 0; i < 5; i++)
{
    var result = interpreter.Tick(ref bb, ref state, ref ctx);
    Console.WriteLine($"  Tick {i + 1}: {result}  AmmoCount={bb.AmmoCount}");
}
