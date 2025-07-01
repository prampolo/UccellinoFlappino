using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Assets.SerialPortUtility.Scripts;

public class PlayerMove : MonoBehaviour
{
    [Header("References")]
    // Assign this in the Inspector: the GameObject that holds the GameplayScript component
    public GameObject gameManager;
    // Singleton for CameraScript and others
    public static PlayerMove instance;
    public bool isAlive = true;

    [Header("Movement Settings")]
    public float forwardSpeed = 3f;

    [Header("Positions (Y world)")]
    public float sittingY = -3f;
    public float standingY = 3f;
    public float moveDuration = 1f;

    [Header("Thresholds")]
    public float threshold = 400f;

    [Header("Health Settings")]
    public int health = 3;            // Number of lives
    public Text healthText;           // UI text for health display

    [Header("Score Settings")]
    public Text scoreText;   // assegna da Inspector il Text UI per lo score
    private int score;       // valore corrente dello score

    [Header("Serial Port Settings")]
    public string serialPortName = "COM1";
    public int baudRate = 9600;

    private SerialCommunicationFacade serialFacade;
    private enum State { Unknown, Sitting, Standing }
    private State currentState = State.Unknown;
    private bool isMoving = false;

    // Physics
    private Rigidbody2D rb;

    // Thread safety for serial callbacks
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private readonly object queueLock = new object();

    void Awake()
    {
        // Reference check for gameManager
        if (gameManager == null)
            Debug.LogError("PlayerMove: gameManager reference not set. Please assign in Inspector.");
        // Physics setup
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    void Start()
    {
        // Initialize health UI
        if (healthText != null)
            healthText.text = "X" + health;

        score = 0;
        if (scoreText != null)
            scoreText.text = score.ToString();

        // Initialize serial communication
        serialFacade = new SerialCommunicationFacade();
        try
        {
            serialFacade.Connect(baudRate, serialPortName);
            serialFacade.OnSerialMessageReceived += OnSerialData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SERIAL] Cannot open port {serialPortName}: {ex.Message}");
        }
    }

    void Update()
    {
        // Execute queued actions
        lock (queueLock)
        {
            while (mainThreadActions.Count > 0)
                mainThreadActions.Dequeue()?.Invoke();
        }
    }

    void FixedUpdate()
    {
        // Constant forward movement via Rigidbody2D
        if (isAlive && rb != null)
        {
            Vector2 newPos = rb.position + Vector2.right * forwardSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }
    }

    void OnSerialData(byte[] data)
    {
        string raw = Encoding.ASCII.GetString(data).Trim();
        string[] parts = raw.Split(';');
        string[] rightS, leftS;

        if (parts.Length == 3)
        {
            rightS = parts[1].Split(',');
            leftS = parts[2].Split(',');
        }
        else if (parts.Length == 2)
        {
            var all = parts[1].Split(',');
            if (all.Length == 8)
            {
                rightS = all.Take(4).ToArray();
                leftS = all.Skip(4).ToArray();
            }
            else { EnqueueAction(() => Debug.LogWarning($"[SERIAL] Unexpected sensor count: {raw}")); return; }
        }
        else { EnqueueAction(() => Debug.LogWarning($"[SERIAL] Unexpected format: {raw}")); return; }

        try
        {
            float sumR = 0f, sumL = 0f;
            for (int i = 0; i < 4; i++)
            {
                sumR += int.Parse(rightS[i]);
                sumL += int.Parse(leftS[i]);
            }
            float foot_r = sumR / 4f;
            float foot_l = sumL / 4f;
            EnqueueAction(() => ProcessFoot(foot_l, foot_r));
        }
        catch (Exception ex)
        {
            EnqueueAction(() => Debug.LogWarning($"[SERIAL] Parsing error: {ex.Message} on {raw}"));
        }
    }

    private void ProcessFoot(float foot_l, float foot_r)
    {
        Debug.Log($"[SERIAL] foot_l={foot_l:F1}, foot_r={foot_r:F1}");
        if (foot_l < threshold && foot_r < threshold)
        {
            if (currentState != State.Sitting && !isMoving)
                StartCoroutine(MoveToY(sittingY, State.Sitting));
        }
        else if (foot_l > threshold && foot_r > threshold)
        {
            if (currentState != State.Standing && !isMoving)
                StartCoroutine(MoveToY(standingY, State.Standing));
        }
        else
        {
            Debug.Log($"Error: foot_l={foot_l:F1}, foot_r={foot_r:F1}");
        }
    }

    private IEnumerator MoveToY(float targetY, State newState)
    {
        isMoving = true;
        Vector2 start = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 end = new Vector2(start.x, targetY);
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            Vector2 pos = Vector2.Lerp(start, end, elapsed / moveDuration);
            if (rb != null) rb.MovePosition(pos);
            else transform.position = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.MovePosition(end);
        else transform.position = end;

        currentState = newState;
        isMoving = false;
        Debug.Log($"Position: {end} — {(newState == State.Sitting ? "Sitting" : "Standing")}   ");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[COLLISION] Collided with {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Destroy(collision.gameObject);
            health--;
            Debug.Log($"[HEALTH] Lost a life! Remaining: {health}");
            if (healthText != null) healthText.text = "X" + health;
            if (health <= 0)
            {
                isAlive = false;
                Debug.Log("[GAME] Player has died.");
                // Close serial communication on Game Over
                serialFacade?.Disconnect();
                // Close serial communication on Game Over
                serialFacade?.Disconnect();
                // Show Game Over UI
                if (gameManager != null)
                    gameManager.GetComponent<GameplayScript>().PauseGame();
                else
                    Debug.LogError("PlayerMove: Cannot pause game, gameManager is null.");
                StartCoroutine(StopMoving());
            }
        }
        if (collision.gameObject.CompareTag("Life"))
        {
            Destroy(collision.gameObject);
            health++;
            Debug.Log($"[HEALTH] Get a life! Remaining: {health}");
            if (healthText != null) healthText.text = "X" + health;
        }
        if (collision.gameObject.CompareTag("Coin"))
        {
            Destroy(collision.gameObject);
            score++;
            Debug.Log($"[SCORE] Collected coin. Score: {score}");
            if (scoreText != null) scoreText.text = score.ToString();
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        Debug.Log($"[TRIGGER] Triggered by {collider.gameObject.name} (Tag: {collider.gameObject.tag})");
        if (collider.CompareTag("Enemy"))
        {
            Destroy(collider.gameObject);
            health--;
            Debug.Log($"[HEALTH] Lost a life! Remaining: {health}");
            if (healthText != null) healthText.text = "X" + health;
            if (health <= 0)
            {
                isAlive = false;
                Debug.Log("[GAME] Player has died.");
                GameplayScript.instance.GameOver();
                GameplayScript.instance.IfGameIsOver(score: 0);
                StartCoroutine(StopMoving());
            }
        }
        if (collider.CompareTag("Life"))
        {
            Destroy(collider.gameObject);
            health++;
            Debug.Log($"[HEALTH] Get a life! Remaining: {health}");
            if (healthText != null) healthText.text = "X" + health;
        }
        if (collider.CompareTag("Coin"))
        {
            Destroy(collider.gameObject);
            score++;
            Debug.Log($"[SCORE] Collected coin. Score: {score}");
            if (scoreText != null) scoreText.text = score.ToString();
        }
    }

    /// <summary>
    /// Coroutine to pause game after showing Game Over
    /// </summary>
    private IEnumerator StopMoving()
    {
        yield return new WaitForSeconds(1.5f);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Exposed to CameraScript for following.
    /// </summary>
    public float GetPositionX() => rb != null ? rb.position.x : transform.position.x;

    void OnApplicationQuit() => serialFacade?.Disconnect();

    // Ensure serial port is closed when this component is disabled
    void OnDisable()
    {
        if (serialFacade != null)
        {
            serialFacade.Disconnect();
            Debug.Log("[SERIAL] Disconnected on disable.");
        }
    }

    private void EnqueueAction(Action a)
    {
        lock (queueLock) { mainThreadActions.Enqueue(a); }
    }
}
