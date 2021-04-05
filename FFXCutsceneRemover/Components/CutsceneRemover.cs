﻿using FFX_Cutscene_Remover.ComponentUtil;
using FFXCutsceneRemover.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

/*
 * Main loops for the Cutscene Remover program.
 */
namespace FFXCutsceneRemover
{
    class CutsceneRemover
    {
        private static readonly string TARGET_NAME = "FFX";

        // Print out the name and value of every memory
        // address each iteration of the main loop
        private readonly bool PrintDebugValues = true;

        private readonly MemoryWatchers MemoryWatchers = MemoryWatchers.Instance;

        private Process Game;
        private bool InBossFight = false;
        private Transition PostBossFightTransition;
        // Keep track of the previously executed transition
        // so we don't execute the same transition twice
        private Transition PreviouslyExecutedTransition;
        private int LoopSleepMillis;

        public CutsceneRemover(bool debug, int loopSleepMillis)
        {
            PrintDebugValues = debug;
            LoopSleepMillis = loopSleepMillis;
        }

        public void MainLoop()
        {
            while (true)
            {
                ConnectToTarget();

                if (Game == null)
                {
                    continue;
                }

                MemoryWatchers.Initialize(Game);

                Console.WriteLine("Starting main loop!");
                while (!Game.HasExited)
                {

                    // Update the values of our memory watchers.
                    // This is really important.
                    MemoryWatchers.Watchers.UpdateAll(Game);

                    if (PrintDebugValues)
                    {
                        MemoryWatcherList watchers = MemoryWatchers.Watchers;

                        // Report the current status of all of our watched memory. For debug purposes
                        foreach (MemoryWatcher watcher in watchers)
                        {
                            Console.WriteLine(watcher.Name + ": " + watcher.Current);
                        }
                        Console.Write("InBossFight: " + InBossFight);
                    }

                    /* This loop iterates over the list of standard transitions
                     * and applies them when necessary. Most transitions can be performed here.*/
                    Dictionary<IGameState, Transition> standardTransitions = Transitions.StandardTransitions;
                    foreach (var transition in standardTransitions)
                    {
                        if (transition.Key.CheckState() && MemoryWatchers.ForceLoad.Current == 0)
                        {
                            ExecuteTransition(transition.Value, "Executing Standard Transition - No Description");
                        }
                    }

                    
                    /* Loop for post boss fights transitions. Once we enter the fight we set the boss bit and the transition
                     * to perform once we exit the AP menu. */
                    Dictionary<IGameState, Transition> postBossBattleTransitions = Transitions.PostBossBattleTransitions;
                    if (!InBossFight)
                    {
                        foreach (var transition in postBossBattleTransitions)
                        {
                            if (transition.Key.CheckState())
                            {
                                InBossFight = true;
                                PostBossFightTransition = transition.Value;
                                Console.WriteLine("Entered Boss Fight: " + transition.Value.Description);
                            }
                        }
                    }
                    else if (InBossFight && new GameState {RoomNumber = 23}.CheckState())
                    {
                        Console.WriteLine("Main menu detected. Exiting boss loop (This means you died or soft-reset)");
                        InBossFight = false;
                    }
                    else if (new GameState { Menu = 0 }.CheckState() && new PreviousGameState { Menu = 1 }.CheckState())
                    {;
                        ExecuteTransition(PostBossFightTransition, "Executing Post Boss Fight Transition - No Description");
                        InBossFight = false;
                        PostBossFightTransition = null;
                    }

                    // SPECIAL CHECKS
                    /*
                     * A GameState object is created in order to verify the current state of the game
                     * based on the inputs provided. Inputs not provided will be ignored.
                     * Once the CheckState() returns true, indicating the game is in the state we want,
                     * then a Transition object is created with inputs required to execute the transition.
                     * The Execute() method causes the transition to write the updated values to memory.
                     *
                     * IF YOU ONLY NEED TO CHECK IF ADDRESSES ARE CERTAIN VALUES (ALMOST ALL CASES), THEN ADD YOUR
                     * TRANSITION INTO THE 'Resources\Transitions.cs' FILE.
                     *
                     * Rarely there are conditions where the check we want is not equality. In that case you can write your
                     * own condition. An example is below.
                     * 
                     * Make sure to call ExecuteTransition() instead of calling the Transition.Execute() method directly.
                     */
                    // Soft reset by holding L1 R1 L2 R2 + Start - Disabled in battle because game crashes
                    if (new GameState { Input = 2063 }.CheckState() && MemoryWatchers.BattleState.Current != 10)
                    {
                        ExecuteTransition(new Transition { RoomNumber = 23, BattleState = 778, Description = "Soft reset by holding L1 R1 L2 R2 + Start" });
                    }
                    
                    // Custom Check #1 - Sandragoras
                    if (new GameState { RoomNumber = 138, Storyline = 1720, State = 1}.CheckState() && MemoryWatchers.Sandragoras.Current >= 4)
                    {
                        ExecuteTransition(new Transition { RoomNumber = 130, Storyline = 1800, SpawnPoint = 0, Description = "Sanubia to Home"});
                    }
                    
                    // Custom Check #2 - Airship
                    if (new GameState { RoomNumber = 194, Storyline = 2000, State = 0}.CheckState() && MemoryWatchers.XCoordinate.Current > 300f)
                    {
                        ExecuteTransition(new Transition {RoomNumber = 194, Storyline = 2020, SpawnPoint = 1, Description = "Zoom in on Bevelle"});
                    }

                    // Custom Check #3 - Buff Brotherhood in Farplane and skip scenes
                    if (new GameState { RoomNumber = 193, Storyline = 1154 }.CheckState())
                    {
                        Game.Suspend();
                        IntPtr EquipMenu = new IntPtr(MemoryWatchers.GetBaseAddress() + 0xD30F2C); // Address of beginning of Equipment menu
                        bool foundBrotherhood = false;
                        var brotherhood = new byte[2] { 0x1, 0x50 }; // Brotherhood name identifier in hex

                        while (!foundBrotherhood)
                        {
                            // Check first two bytes for name identifier and compare against Brotherhood
                            var equipment = Game.ReadBytes(EquipMenu, 2);

                            if (equipment.SequenceEqual<byte>(brotherhood))
                            {
                                // Not sure what this value is, but it does change during the scene, so adding just in case!
                                IntPtr aNumber = IntPtr.Add(EquipMenu, 3);
                                Game.WriteBytes(aNumber, new byte[1] { 0x9 });

                                // Second slot for Brotherhood, +10% Strength
                                IntPtr slot2 = IntPtr.Add(EquipMenu, 16);
                                Game.WriteBytes(slot2, new byte[2] { 0x64, 0x80 });

                                // Third slot for Brotherhood, Waterstrike
                                IntPtr slot3 = IntPtr.Add(EquipMenu, 18);
                                Game.WriteBytes(slot3, new byte[2] { 0x2A, 0x80 });

                                // Fourth slot for Brotherhood, Sensor
                                IntPtr slot4 = IntPtr.Add(EquipMenu, 20);
                                Game.WriteBytes(slot4, new byte[2] { 0x00, 0x80 });

                                // Finally skip the Farplane scenes
                                new Transition { RoomNumber = 134, Storyline = 1170 }.Execute();
                                Console.WriteLine("Farplane scenes + Brotherhood buff");
                                foundBrotherhood = true;
                                Game.Resume();
                                break;
                            }
                            else
                            {
                                // Number of bytes for each piece of equipment is 22, so if not found, go to the next piece of equipment
                                EquipMenu = IntPtr.Add(EquipMenu, 22);
                            }
                        }
                    }

                    // Sleep for a bit so we don't destroy CPUs
                    Thread.Sleep(LoopSleepMillis);
                }
            }
        }

        // Save the previous transition so that we don't execute the same transition multiple times in a row.
        private void ExecuteTransition(Transition transition, string defaultDescription = "")
        {
            if (transition != PreviouslyExecutedTransition)
            {
                Game.Suspend();
                transition.Execute(defaultDescription);
                PreviouslyExecutedTransition = transition;
                Game.Resume();
            }
        }

        private void ConnectToTarget()
        {
            Console.WriteLine("Connecting to FFX...");
            try
            {
                Game = Process.GetProcessesByName(TARGET_NAME).OrderByDescending(x => x.StartTime)
                         .FirstOrDefault(x => !x.HasExited);
            }
            catch (Win32Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }

            if (Game == null || Game.HasExited)
            {
                Game = null;
                Console.WriteLine("FFX not found! Waiting for 10 seconds.");

                Thread.Sleep(10 * 1000);
            }
            else
            {
                Console.WriteLine("Connected to FFX!");
            }
        }
    }
}
