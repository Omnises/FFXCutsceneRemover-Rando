namespace FFXCutsceneRemover;

class BFATransition : Transition
{
    public override void Execute(string defaultDescription = "")
    {
        if (base.Stage == 0)
        {
            base.Execute();

            BaseCutsceneValue = MemoryWatchers.EventFileStart.Current;
            base.Stage += 1;

        }
        else if (MemoryWatchers.BFATransition.Current >= (BaseCutsceneValue + 0x42) && Stage == 1)
        {
            //WriteValue<int>(MemoryWatchers.BFATransition, BaseCutsceneValue + 0xE1); //Not currently working as desired
            Stage += 1;
        }
        else if (MemoryWatchers.BFATransition.Current >= (BaseCutsceneValue + 0xC08C) && Stage == 2) // 0xA3E5 in event script
        {
            WriteValue<int>(MemoryWatchers.BFATransition, BaseCutsceneValue + 0xCDBB); // 0xAE93 in event script
            Stage += 1;
        }
        else if (MemoryWatchers.BFATransition.Current >= (BaseCutsceneValue + 0xCDC4) && Stage == 3) // 0xAE9C in event script
        {
            WriteValue<int>(MemoryWatchers.BFATransition, BaseCutsceneValue + 0xCF98); // 0xB070 in event script
            Stage += 1;
        }
        else if (MemoryWatchers.BFATransition.Current >= (BaseCutsceneValue + 0xCFB9) && Stage == 4) // 0xB091 in event script
        {
            WriteValue<int>(MemoryWatchers.BFATransition, BaseCutsceneValue + 0xD126); // 0xB1FE in event script
            Stage += 1;
        }
        else if (MemoryWatchers.BFATransition.Current >= (BaseCutsceneValue + 0xD12C) && Stage == 5) // 0xB204 in event script
        {
            WriteValue<int>(MemoryWatchers.BFATransition, BaseCutsceneValue + 0xD382); // 0xB45A in event script
            Stage += 1;
        }
    }
}