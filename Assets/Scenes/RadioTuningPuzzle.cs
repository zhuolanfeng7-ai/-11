using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public sealed class RadioTuningPuzzle : MonoBehaviour
{
    [SerializeField] private RadioQuarterTurn first,second,third;
    [SerializeField] private UnityEvent onSolved=new UnityEvent();
    public bool IsSolved => first && second && third && first.IsAtTarget && second.IsAtTarget && third.IsAtTarget;
    public bool HasCompleted {get;private set;}
    public void Configure(RadioQuarterTurn a,RadioQuarterTurn b,RadioQuarterTurn c){first=a;second=b;third=c;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForScene()
    {
        if(!string.Equals(SceneManager.GetActiveScene().name.Replace(" ",""),"scene2",System.StringComparison.OrdinalIgnoreCase))return;
        if(FindObjectOfType<RadioTuningPuzzle>())return;
        var a=GameObject.Find("botton1");var b=GameObject.Find("botton2");var c=GameObject.Find("botton3");if(!a||!b||!c)return;
        new GameObject("Radio Tuning Puzzle").AddComponent<RadioTuningPuzzle>().Configure(a.GetComponent<RadioQuarterTurn>(),b.GetComponent<RadioQuarterTurn>(),c.GetComponent<RadioQuarterTurn>());
    }
    private void Update(){if(!HasCompleted && IsSolved){HasCompleted=true;onSolved.Invoke();}}
}
