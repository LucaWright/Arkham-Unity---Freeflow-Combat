using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CombatDirectorState { Planning, Dispatching, Executing}
public class CombatDirector : MonoBehaviour
{
    public static CombatDirectorState state = CombatDirectorState.Planning;

    [SerializeField] DistanceHandler distanceHandler = new DistanceHandler();
    
    public static DistanceHandler DistanceInfo;
    
    static List<AgentAI>[] agents; //prima... li prende tutti. Poi li mette in una lista separata per linea. Questi sono i nemici in Idle
    public static Queue<AgentAI>[] attackingCandidates; //Solo linee disponibili! E se mi convenisse sempre fare una lista, in modo tale da eliminare chi mi pare?
    public static List<AgentAI> strikers;


    float tick;
    public float checkEachXSeconds = 0.5f;

    private void Awake()
    {
        distanceHandler.Target = GameObject.FindGameObjectWithTag("Player").transform;
        DistanceInfo = distanceHandler;        
    }

    private void OnEnable()
    {
        InitializeAgentLists();
        strikers = new List<AgentAI>();
    }

    private void Start()
    {

    }

    public static void ChangeLineList(AgentAI agent, int oldLine, int currentLine)
    {
        if (oldLine >= 0)
            agents[oldLine].Remove(agent);

        agents[currentLine].Add(agent);
    }

    void InitializeAgentLists()
    {
        agents = new List<AgentAI>[(distanceHandler.Lines + 2)]; //+2: +1 for line 0 and +1 for the line beyond the last one

        for (int i = 0; i < agents.Length; i++) //Inizializza ogni singola lista negli array
        {
            agents[i] = new List<AgentAI>();
        }
    }

    private void FixedUpdate()
    {
        tick += Time.fixedDeltaTime;

        switch (state)
        {
            case CombatDirectorState.Planning:
                OnUpdatePlanning();
                break;
            case CombatDirectorState.Dispatching:
                OnUpdateDispatch();
                break;
            case CombatDirectorState.Executing:
                //OnUpdateExecuting();
                break;
            default:
                break;
        }
    }

    void OnUpdatePlanning()
    {        
        for (int i = 0; i < agents.Length; i++)
        {
            var elementsCount = agents[i].Count; //anziché prenderli da qui, li prende direttamente da agents?

            if (elementsCount == 0) continue;

            var randomNumber = Random.Range(0, Mathf.Clamp(elementsCount, 1, 3)); //Pesca un numer da 0 a 3

            if (randomNumber == 0) continue; //Se è zero, ricomincia il ciclo;

            int candidates = 0;

            foreach(AgentAI agent in agents[i].ToList()) //C'è questa funzione in più, che cicla e controlla i candidati per ogni lista su ogni linea
            {
                if (agent.isCandidate)
                    candidates += 1;
            }

            if (candidates >= randomNumber)
            {
                for (int j = 0; j < elementsCount; j++)
                {
                    var agent = agents[i].ElementAt(0);
                    agents[i].Remove(agent);
                    strikers.Add(agent);
                    //Cambia stato dell'agent!
                    agent.state = AgentState.Dispatching;
                }
                state = CombatDirectorState.Dispatching;
            }
            return;
        }
    }

    void OnUpdateDispatch()
    {
        //if (strikers.Count == 0)
        //{
        //    ReturnToPlanningPhase();
        //    return;
        //}

        //OnUpdateLocomotion();

        //foreach (AIBehavior foe in strikers)
        //{
        //    foe.OnAttacking();
        //}

        //if (AllStrikersReady())
        //{
        //    foreach (AIBehavior foe in strikers)
        //    {
        //        //attack
        //        foe.Attack();
        //        state = DirectorState.Executing;
        //        foe.state = AIState.Executing;
        //    }
        //}
    }

    //void OnUpdateExecuting()
    //{
    //    //Controlli di sicurezza?
    //    if (strikers.Count == 0)
    //    {
    //        ReturnToPlanningPhase();
    //        return;
    //    }
    //}



    //void InitializeFoeQueues()
    //{
    //    attackingCandidates = new Queue<AIBehavior>[distanceHandler.GetLinesNumber()]; //in teoria, quelli oltre l'ultima linea non possono essere presi come candidati.
    //    for (int i = 0; i < attackingCandidates.Length; i++)
    //    {
    //        attackingCandidates[i] = new Queue<AIBehavior>();
    //    }
    //}

    //public void ClearFoeQueues()
    //{
    //    for (int i = 0; i < attackingCandidates.Length; i++)
    //    {
    //        attackingCandidates[i].Clear();
    //    }
    //}

    //Vector2Int Pick2Randoms()
    //{
    //    List<int> numbers = new List<int>();

    //    for (int i = 1; i < agents.Length; i++)
    //    {
    //        if (agents[i].Count > 0)
    //            numbers.Add(i);
    //    }

    //    if (numbers.Count == 0) return new Vector2Int(-1, -1);

    //    var randomLine = Random.Range(0, numbers.Count); //il max del random è esclusivo, quindi +1

    //    return new Vector2Int(numbers.ElementAt(randomLine), 0);
    //}

    //void PlanAttack() //DECIDI: o Enque lo fa qui, o lo fa quando è in locomotion
    //{
    //    for (int i = 0; i < attackingCandidates.Length; i++)
    //    {
    //        var elementsCount = attackingCandidates[i].Count;

    //        if (elementsCount == 0) continue;

    //        var randomNumber = Random.Range(0, Mathf.Clamp(elementsCount, 1, 3)); //Pesca un numer da 0 a 3

    //        if (randomNumber == 0) continue; //Se è zero, ricomincia il ciclo;

    //        for (int j = 0; j < elementsCount; j++) //Altrimenti, cicla quel numero e mette i primi in coda nella lista degli attaccanti. E si parte
    //        {
    //            var foe = attackingCandidates[i].Dequeue(); //rimuovi dalla coda
    //            FindListState(foe).Remove(foe); //rimuovi dalla lista il candidato pescato (in qualsiasi lista si trovi)
    //            strikers.Add(foe); //aggiunti alla lista degli attaccanti (a cui deve anche accedere il player per le sue funzioni di contrattacco)
    //            foe.Dispatch();
    //        }
    //        return;
    //    }
    //    //state = DirectorState.Dispatch;
    //}

    //int ChooseStrikersLine() //potrebbe essere una sega mentale. Magari cerca solo tizi nella prima o seconda linea e li manda.
    //{
    //    for (int i = 0; i < attackingCandidates.Length; i++)
    //    {
    //        if (attackingCandidates[i].Count > 0)
    //            return i;
    //    }
    //    return -1;
    //}

    //bool AllStrikersReady()
    //{
    //    foreach (AIBehavior foe in strikers)
    //    {
    //        if (!foe.CanAttack()) return false;
    //    }

    //    return true;
    //}

    //public void ReturnToPlanningPhase() //valuta una static
    //{
    //    ClearFoeQueues();
    //    state = DirectorState.Planning; //C'è un errore nei passaggi di stato
    //}

    //public List<AIBehavior> FindListState(AIBehavior foe)
    //{
    //    for (int i = 0; i < agents.Length; i++)
    //    {
    //        if (agents[i].Contains(foe))
    //        {
    //            return agents[i];
    //        }
    //    }

    //    if (locomotionState.Contains(foe))
    //    {
    //        return locomotionState;
    //    }
    //    else
    //    if (strikers.Contains(foe))
    //    {
    //        return strikers;
    //    }
    //    else
    //    if (recoilState.Contains(foe))
    //    {
    //        return recoilState;
    //    }
    //    return recoverState; //non può prendere tizi in recover state. Penso.
    //}

    //public void IdleListDebugger()
    //{
    //    for (int i = 0; i < agents.Length; i++)
    //    {
    //        int count = 0;

    //        foreach (AIBehavior foe in agents[i])
    //        {
    //            count++;
    //        }
    //        Debug.Log("Numero di agent alla linea " + i + ": " + count);
    //    }
    //}

    //void OnUpdateRecoil()
    //{
    //    if (recoilState.Count > 0)
    //    {
    //        foreach (AIBehavior foe in recoilState.ToList())
    //        {
    //            foe.Recoil();
    //        }
    //    }
    //}

    //void OnUpdateGetLines()
    //{
    //    for (int i = 1; i < agents.Length; i++)
    //    {
    //        foreach (AIBehavior foe in agents[i].ToList())
    //        {
    //            var newLine = foe.GetCurrentLine();

    //            if (newLine != i)
    //            {
    //                agents[i].Remove(foe);
    //                agents[newLine].Add(foe); //questo porterà alcuni a essere ciclati più di una volta, ma chissene
    //            }
    //        }
    //    }
    //}

    //void StopLocomotion()
    //{
    //    foreach (AIBehavior foe in locomotionState.ToList())
    //    {
    //        foe.ResetAnimationBools();
    //    }
    //}

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.blue;
        //for (int i = 0; i < Handler.Lines; i++)
        //{
        //    Gizmos.DrawWireSphere(Handler.Target.position, Handler.FirstLineDistance + Handler.LineToLineDistance * i);
        //}

        Gizmos.color = Color.blue;
        for (int i = 0; i < distanceHandler.Lines; i++)
        {
            Gizmos.DrawWireSphere(distanceHandler.Target.position, distanceHandler.FirstLineDistance + distanceHandler.LineToLineDistance * i);
        }
    }

}
