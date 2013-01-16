
regasm /tlb "Microsoft.VisualStudio.Shell.Interop.dll"
regasm /tlb "Pocioci.VS.dll"
gacutil /i "Pocioci.VS.dll"

REGEDIT /S "Pocioci.VS.reg"

devenv /setup