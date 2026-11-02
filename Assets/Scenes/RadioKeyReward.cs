using UnityEngine;

[DisallowMultipleComponent]
public sealed class RadioKeyReward : MonoBehaviour
{
    [SerializeField] private MusicBoxRewardLift keyReward;
    private RadioRecordPlayer recordPlayer;
    private RadioTuningPuzzle puzzle;
    private bool triggered;
    public bool HasTriggered => triggered;
    public MusicBoxRewardLift KeyReward => keyReward;
    public void Configure(MusicBoxRewardLift reward){keyReward=reward;}
    private void Awake(){recordPlayer=GetComponent<RadioRecordPlayer>();}
    private void Update()
    {
        if(triggered || !keyReward || !recordPlayer || !recordPlayer.HasPlacedRecord)return;
        if(!puzzle)puzzle=FindObjectOfType<RadioTuningPuzzle>();
        // Both conditions must be true now; an earlier correct setting alone is insufficient.
        if(!puzzle || !puzzle.IsSolved)return;
        triggered=true;keyReward.Raise();
    }
}
