using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CombatDirectorState { Planning, Dispatching, Executing}
public class CombatDirector : MonoBehaviour
{
    public static CombatDirectorState state = CombatDirectorState.Planning;

    public string combatState = "";
    public int maxEnemyCount = 3;

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
        for (int i = 1; i < (agents.Length - 1); i++)
        {
            foreach (AgentAI agent in agents[i].ToList())
            {
                if (agent.state == AgentState.Idle ||
                    agent.state == AgentState.Positioning)
                {
                    //RaycastHit? hit = CheckAgentLineOfSight(agent);
                    //if (hit.HasValue)
                    //{
                    //    var hitInfo = (RaycastHit)hit;

                    //    if (hitInfo.transform.tag == "Player")
                    //    {
                    //        AddToStrikersCandidateList(agent, i);
                    //    }
                    //}
                    if (agent.hasGreenLightToTarget) //Se cuò manipolasse un bool in agent??? Così non deve rifare il controllo!
                        AddToStrikersCandidateList(agent, i);
                }                
            }
        }
    }

    void PlanAttack()
    {
        var randomNumber = Random.Range(1, maxEnemyCount + 1);

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
                    candidate.StopAllCoroutines();
                    candidate.hasGreenLightToTarget = false; //Serve davvero?
                    candidate.fsm.State = candidate.dispatchingState;
                }

                state = CombatDirectorState.Dispatching;
                return;
            }
        }
    }

    void RetreatNonStrikersFirstLine()
    {
        foreach (AgentAI agent in agents[1])
        {
            if (strikers.Contains(agent)) continue;
            
            //StartCoroutine(agent.PullBack(2)); //Pone il problema dell'avvisare al tizio dietro di levarsi dai coglioni!
            agent.PullBack(2); //Pone il problema dell'avvisare al tizio dietro di levarsi dai coglioni!

            //if (agent.state == AgentState.Idle)
            //{
            //    StartCoroutine(agent.PullBack1(2));
            //}
            //else
            //if (agent.state == AgentState.Positioning)
            //{
            //    StartCoroutine(agent.BackToIdle1());
            //    StartCoroutine(agent.PullBack1(2));
            //}
        }
    }

    public RaycastHit? CheckAgentLineOfSight(AgentAI agent)
    {
        Vector3 raycastOrigin = agent.transform.position + Vector3.up;
        RaycastHit hitInfo;
        if (Physics.SphereCast(raycastOrigin, agent.agentNM.radius, agent.transform.forward, out hitInfo, CombatDirector.DistanceInfo.LastLineRadius, agent.agentLineOfSightLM))
        {
            return hitInfo;
        }
        return null;
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

    //void SetAgentsPriorityOrder()
    //{
    //    int timeStepDelta = 0;
        
    //    for (int i = 1; i < agents.Length; i++)
    //    {
    //        foreach (AgentAI agent in agents[i].ToList())
    //        {
    //            timeStepDelta++;
    //            agent.idleState.priority = timeStepDelta;
    //        }
    //    }
    //}

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
        Debug.Log("Numero strikers: " +strikers.Count);
        if (AllStrikersReady()) //EVENT
        {
            foreach (AgentAI agent in strikers)
            {
                agent.fsm.State = agent.attackingState;
                Debug.Log("Counter: " +agent.gameObject.name); //TODO verificare l'errore del false counter
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

    //private void OnDrawGizmos()
    //{
    //    //Gizmos.color = Color.blue;
    //    //for (int i = 0; i < Handler.Lines; i++)
    //    //{
    //    //    Gizmos.DrawWireSphere(Handler.Target.position, Handler.FirstLineDistance + Handler.LineToLineDistance * i);
    //    //}

    //    Gizmos.color = Color.blue;
    //    for (int i = 0; i < distanceHandler.Lines; i++)
    //    {
    //        Gizmos.DrawWireSphere(distanceHandler.Target.position, distanceHandler.FirstLineDistance + distanceHandler.LineToLineDistance * i);
    //    }
    //}

}
