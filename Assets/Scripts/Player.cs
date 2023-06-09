using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public CharacterController controller;

    public float speed = 12f;

    Vector3 velocity;
    public float gravity = -9.81f;
    public float jumpHeight = 3;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    bool isGrounded;

    Vector2 movement;
    Vector2 view;
    public float mouseSensitivity = 5000;
    float xRotation = 0f;
    public Camera cam;

    bool showMouse = false;

    public Button reset;
    public Button quit;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        reset.onClick.AddListener(ResetApp);
        quit.onClick.AddListener(QuitApp);
    }

    void Update()
    {
        movement.x = Input.GetAxis("Horizontal");
        movement.y = Input.GetAxis("Vertical");
        view.x = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        view.y = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= view.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (Input.GetButtonDown("Cancel"))
        {
            showMouse = !showMouse;

        }

        if (showMouse)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
            transform.Rotate(Vector3.up * view.x);
        }
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2;
        }

        Vector3 move = transform.right * movement.x + transform.forward * movement.y;
        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void ResetApp()
    {
        SceneManager.LoadScene(0);
    }
    private void QuitApp()
    {
        Application.Quit();
    }

}




