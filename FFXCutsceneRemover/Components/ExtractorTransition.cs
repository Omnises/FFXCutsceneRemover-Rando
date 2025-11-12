using FFXCutsceneRemover.Logging;
using System.Collections.Generic;

namespace FFXCutsceneRemover;

class ExtractorTransition : Transition
{
    static private List<short> CutsceneAltList = new List<short>(new short[] { 1137 });
    public override void Execute(string defaultDescription = "")
    {
        if (MemoryWatchers.ExtractorTransition.Current > 0)
        {
            if (Stage == 0)
            {
                base.Execute();

                BaseCutsceneValue = MemoryWatchers.EventFileStart.Current;
                Stage += 1;

            }
            else if (MemoryWatchers.ExtractorTransition.Current == (BaseCutsceneValue + 0x16CA) && MemoryWatchers.BattleState2.Current == 1 && Stage == 1)
            {
                WriteValue<int>(MemoryWatchers.ExtractorTransition, BaseCutsceneValue + 0x1772);
                Stage += 1;
            }
        }
    }
}