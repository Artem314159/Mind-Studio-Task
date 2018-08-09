using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    //File with best dcore
    string bestScorePath = "Assets/Scenes/Best score";

    //GameObjects
    Player Player;
    public GameObject PlayerFrame;
    public GameObject CarFrame;
    public Camera myCamera;
    public GameObject myPlane;
    public Canvas StartMenu;
    public Canvas AgainMenu;

    //List for generation of cars
    private List<Car> Cars = new List<Car>();

    private Dictionary<string, Vector3> startPosition = new Dictionary<string, Vector3>
        { { "myCamera", new Vector3(0, 10, 0) }, { "myPlane", new Vector3(-20, 0, 20) }, { "Player", new Vector3(-5, 0.51f, 4) } };
    private bool GameIsStarted = false;
    private float sceneSpeed = 1.5f;
    int bestScore = 0;
    
    void Start()
    {
        //Read best score
        StreamReader reader = new StreamReader(bestScorePath);
        char[] strToChar = reader.ReadToEnd().ToCharArray();
        reader.Close();
        for (int i = 0; i < strToChar.Length; i++)
        {
            char letter = strToChar[i];
            bestScore = 10 * bestScore + (letter - 48);
        }

        //Display best Score
        Transform child = StartMenu.transform.Find("Best Score");
        child.GetComponent<Text>().text = "The best score: " + bestScore;
        child = AgainMenu.transform.Find("Best Score");
        child.GetComponent<Text>().text = "The best score: " + bestScore;

        //prepare to run program
        AgainMenu.enabled = false;
        Player = new Player(PlayerFrame);

        Pool.Prefab = CarFrame;
        PoolInitialization();
        StartCoroutine(CarsGenerator());
    }

    void Update()
    {
        //Moving of cars
        foreach (Car currCar in Cars.ToArray())
        {
            currCar.MoveForward();
            if (currCar.IsAway())
            {
                Cars.Remove(currCar);
                currCar.Delete();
            }
        }
        if (GameIsStarted)
        {
            //Moving of camera and plane
            if (myCamera.transform.position.z > Player.Position.z + 2)
                OnTriggerEnter(new Collider());
            else if (myCamera.transform.position.z < Player.Position.z - 5)
                sceneSpeed = 10f;
            else sceneSpeed = 2f;
            myCamera.transform.position += Vector3.forward * sceneSpeed * Time.deltaTime;
            myPlane.transform.Translate(Vector3.up * sceneSpeed * Time.deltaTime);

            //Movement by pressing
            if (Input.GetKeyDown(KeyCode.Mouse0) || (Input.touchCount > 0 && 
                Input.GetTouch(Input.touchCount - 1).phase == TouchPhase.Began))
            {
                StartCoroutine(Player.RunForward());
            }
        }
    }

    public void PoolInitialization()
    {
        for (int i = 0; i < 50; i++)
        {
            Pool.NonActive.Push(Instantiate(Pool.Prefab));
            Pool.NonActive.Peek().SetActive(false);
        }
    }

    //
    public void StartButtonClicked()
    {
        StartMenu.enabled = false;
        Reset();
    }

    public void AgainButtonClicked()
    {
        AgainMenu.enabled = false;
        Reset();
    }

    void Reset()
    {
        GameIsStarted = true;
        Player.Score = 0;
        Car.level = 1;
        myCamera.transform.position = startPosition["myCamera"];
        myPlane.transform.position = startPosition["myPlane"];
        Player.Position = startPosition["Player"];
    }

    IEnumerator CarsGenerator()
    {
        while (true)
        {
            int randCoordZ = Random.Range(1, 5);
            int randDirect = Random.Range(0, 2);
            float randLength = Random.Range(1f, 5f);

            //The last nearest line
            int playerCoord = (((int)Player.Position.z) / 2) * 2;
            Car newCar = new Car(new Vector3(Car.startCoordsX[randDirect], 0.5f,
                 playerCoord + 2 * (2 * randCoordZ + randDirect) - (playerCoord % 4)), randLength, randDirect);
            Cars.Add(newCar);

            yield return new WaitForSeconds(0.5f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        GameIsStarted = false;
        if(other != null)
            Player.Crash();
        AgainMenu.enabled = true;

        Transform childCurr = AgainMenu.transform.Find("Curr Score");
        childCurr.GetComponent<Text>().text = "The score: " + Player.Score;
        
        if (Player.Score > bestScore)
        {
            bestScore = Player.Score;
            Transform childBest = AgainMenu.transform.Find("Best Score");
            childBest.GetComponent<Text>().text = "The best score: " + bestScore;
            StreamWriter writer = new StreamWriter(bestScorePath);
            writer.Write(bestScore);
            writer.Close();
        }
    }
}

public class Pool : MonoBehaviour
{
    public static Stack<GameObject> NonActive = new Stack<GameObject>();
    public static GameObject Prefab;
    public static GameObject Pop()
    {
        GameObject newObj = NonActive.Pop();
        newObj.SetActive(true);
        return newObj;
    }
    public static void Push(GameObject newObj)
    {
        newObj.SetActive(false);
        NonActive.Push(newObj);
    }
}

public class Player
{
    private GameObject frame;
    private int score = 0;
    private Vector3 start = new Vector3(-5f, 0.51f, 4f);
    private float speed = 10f;
    //public static Object lockRun = new Object();
    private static bool lockRun = false;
    public Player(GameObject newFrame)
    {
        frame = newFrame;
        frame.transform.position = start;
        frame.GetComponent<Renderer>().material.color = Color.yellow;
        frame.GetComponent<Collider>().isTrigger = true;
        frame.AddComponent<Rigidbody>();
        frame.GetComponent<Rigidbody>().useGravity = false;
        ParticleSystem.EmissionModule em = frame.GetComponent<ParticleSystem>().emission;
        em.enabled = false;
    }

    public int Score
    {
        get { return score; }
        set { score = value; }
    }

    public Vector3 Position
    {
        get { return frame.transform.position; }
        set { frame.transform.position = value; }
    }
    public void Crash()
    {
        ParticleSystem.EmissionModule em = frame.GetComponent<ParticleSystem>().emission;
        em.enabled = true;
        frame.GetComponent<ParticleSystem>().Play();
    }
    public IEnumerator RunForward()
    {
        if(!lockRun)
        {
            lockRun = true;
            float prevPosZ = frame.transform.position.z;
            while (frame.transform.position.z < prevPosZ + 2)
            {
                frame.transform.Translate(Vector3.forward * speed * Time.deltaTime);
                yield return new WaitForSeconds(0.001f);
            }
            frame.transform.position = new Vector3(frame.transform.position.x, frame.transform.position.y, prevPosZ + 2);
            Score++;
            if (Score % 25 == 0)
                Car.level++;
            lockRun = false;
        }
    }
}

public class Car
{
    private GameObject frame;
    public static float speed = 5f;
    public static int level = 1;
    public static float[] startCoordsX = { -20f, 10f };
    public static float[] rotateAngle = { 180f, 0f };

    public Car(Vector3 coords, float length, int direct)
    {
        frame = Pool.Pop();
        frame.transform.position = coords;
        frame.transform.localScale = new Vector3(length, frame.transform.localScale.y, frame.transform.localScale.z);
        frame.GetComponent<Renderer>().material.color = Color.black;
        frame.transform.Rotate(Vector3.up, rotateAngle[direct]);
    }
    public void MoveForward()
    {
        frame.transform.Translate(Vector3.left * speed * level * Time.deltaTime);
    }
    public bool IsAway()
    {
        return frame.transform.position.x < startCoordsX[0] - 1 ||
            frame.transform.position.x > startCoordsX[1] + 1;
    }
    public void Delete()
    {
        Pool.Push(frame);
    }
}