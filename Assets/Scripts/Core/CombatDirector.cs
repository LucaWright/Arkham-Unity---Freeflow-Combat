using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CombatDirectorState { Planning, Dispatching, Executing}
public class CombatDirector : MonoBehaviour
{
    public static CombatDirectorState state = CombatDirectorState.Planning;

    public string combatState = "";
    public int maxEnemyCount = 3; //Diventa un array in cui devo inserire le percentuali?
    public int[] randomPercentage;

    [SerializeField] DistanceHandler distanceHandler = new DistanceHandler();
    
    public static DistanceHandler DistanceInfo;
    
    static List<AgentAI>[] agents; //prima... li prende tutti. Poi li mette in una lista separata per linea. Questi sono i nemici in Idle
    static List<AgentAI>[] strikersCandidates; //Solo linee disponibili! E se mi convenisse sempre fare una lista, in modo tale da eliminare chi mi pare?
    public static List<AgentAI> strikers;

    public static float strikeStartTime = 0;

    private void Awake()
    {
        distanceHandler.Target = GameObject.FindGameObjectWithTag("Player").transform;
        DistanceInfo = distanceHandler;        
    }

    private void OnEnable()
    {
        InitializeAgentLists();
        InitializeCandidatesLists();
        strikers = new List<AgentAI>();
    }

    private void Start()
    {
        //SetAgentsPriorityOrder();
    }
    private void FixedUpdate() // Una coroutine che chiama le funzioni ogni fixed update e poi... Wait next frame!
    {
        StartCoroutine(Tick());
    }

    IEnumerator Tick()
    {
        /*TODO:
        Le cose devono cambiare un po'.
        1. Un parametro di intensità (magari modificabile da UI)
        2. Il random diventa pseudo-random. Il gioco tiene conto di N ondate precedenti e può forzare il risultato del random di intensità.
        3. Cosa simile per quanto riguarda in numero di scagnozzi che partono.
        4. Fare un test in cui si parte dalla linea più esterna fino a quella più interna.
        */
        yield return new WaitForEndOfFrame();
        //TODO: FIND!
        //agents = agents.OrderBy(x => (this.transform.position - distanceHandler.Target.transform.position).sqrMagnitude);
        
        switch (state)
        {
            case CombatDirectorState.Planning: //Assegna tempistiche diverse al timere di Idle in base a posizionamento!
                combatState = "Planning";
                OnUpdatePlanning();
                break;
            case CombatDirectorState.Dispatching:
                combatState = "Dispatching";
                OnUpdateDispatch();
                break;
            case CombatDirectorState.Executing:
                combatState = "Executing";
                OnUpdateExecuting();
                break;
            default:
                break;
        }
    }

    void OnUpdatePlanning()
    {
        strikers.Clear(); //security check
        LookForCandidates();
        PlanAttack();
        ClearCandidatesLists();
    }

    void LookForCandidates() //È il combat director a guardare per i candidati!
    {
        for (int i = 1; i < (agents.Length - 1); i++) //Esclude a priori quelli sulla linea zero
        {
            foreach (AgentAI agent in agents[i].ToList())
            {
                //if (agent.state == AgentState.Idle ||
                //    agent.state == AgentState.Positioning)
                if (agent.state == AgentState.Locomotion)
                {
                    if (agent.HasGreenLightToTheTarget())
                        AddToStrikersCandidateList(agent, i);
                }                
            }
        }
    }

    int PickRandoms()
    {
        int starting = 0;

        var randomNumber = Random.Range(0, 101);
        for (int i = 0; i < randomPercentage.Length; i++)
        {
            if (randomNumber > starting && randomNumber <= (starting + randomPercentage[i]))
            {
                return (i + 1);
            }
            starting += randomPercentage[i];
        }
        return 1;
    }

    void PlanAttack()
    {
        //var randomNumber = Random.Range(1, maxEnemyCount + 1);
        var randomNumber = PickRandoms();

        for (int i = randomNumber; i > 0; i--)
        {
            for (int j = 0; j < strikersCandidates.Length; j++)
            {
                var candidates = strikersCandidates[j];
                //Debug.Log("Su linea " + (j+1) + " ci sono " + candidates.Count + " candidati.");
                if (candidates.Count < i) continue;

                for (int k = 0; k < i; k++)
                {
                    var candidate = candidates.ElementAt(k);
                    strikers.Add(candidate);
                    //candidate.RunForward(candidate.attackRange);
                    //TODO
                    //Fermare le coroutine in atto?
                    //candidate.StopAllCoroutines();
                    candidate.fsm.State = candidate.dispatchingState;
                }

                state = CombatDirectorState.Dispatching;
                return;
            }
        }
    }

    public static void UpdateAgentLists(AgentAI agent, int oldLine, int currentLine)
    {
        if (oldLine >= 0)
            agents[oldLine].Remove(agent);

        agents[currentLine].Add(agent);
    }

    public static void UpdateCandidateLists(AgentAI agent, int oldLine, int currentLine)
    {
        oldLine = Mathf.Clamp(oldLine, 1, DistanceInfo.Lines -1);
        currentLine = Mathf.Clamp(currentLine, 1, DistanceInfo.Lines);
        
        if (strikersCandidates[oldLine -1].Contains(agent))
        {
            strikersCandidates[oldLine -1].Remove(agent);
            strikersCandidates[currentLine -1].Add(agent);
        }
    }

    public static void AddToStrikersCandidateList(AgentAI agent, int currentLine)
    {
        strikersCandidates[currentLine - 1].Add(agent);
    }

    void ClearCandidatesLists()
    {
        for (int i = 0; i < strikersCandidates.Length; i++)
        {
            strikersCandidates[i].Clear();
        }
    }

    void InitializeAgentLists()
    {
        agents = new List<AgentAI>[(distanceHandler.Lines + 2)]; //+2: +1 for line 0 and +1 for the line beyond the last one

        for (int i = 0; i < agents.Length; i++) //Inizializza ogni singola lista negli array
        {
            agents[i] = new List<AgentAI>();
        }
    }

    void InitializeCandidatesLists()
    {
        strikersCandidates = new List<AgentAI>[distanceHandler.Lines];

        for (int i = 0; i < strikersCandidates.Length; i++) //Inizializza ogni singola lista negli array
        {
            strikersCandidates[i] = new List<AgentAI>();
        }
    }    

    void OnUpdateDispatch()
    {
        if (AllStrikersReady()) //EVENT
        {
            foreach (AgentAI agent in strikers)
            {
                agent.fsm.State = agent.attackingState;
                //Debug.Log("Counter: " +agent.gameObject.name); //TODO verificare l'errore del false counter
            }
            strikeStartTime = Time.time;
            state = CombatDirectorState.Executing;
        }
    }

    void OnUpdateExecuting()
    {
        //Non saprei
        //Magari aspetta segnale di FINE ANIMAZIONE prima di tornare in Planning
        
        ////Controlli di sicurezza?
        //if (strikers.Count == 0)
        //{
        //    ReturnToPlanningPhase();
        //    return;
        //}
    }

    bool AllStrikersReady()
    {
        foreach (AgentAI agent in strikers)
        {
            if (!agent.canAttack) return false;
        }
        return true;
    }
}
