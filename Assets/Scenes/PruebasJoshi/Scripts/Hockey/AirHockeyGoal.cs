using UnityEngine;

public class AirHockeyGoal : MonoBehaviour
{
    [SerializeField] private AirHockeyGameManager gameManager;

    [Tooltip("Actívalo si entrar en esta portería significa que anotó el jugador.")]
    [SerializeField] private bool pointForPlayer;

    private void OnTriggerEnter(Collider other)
    {
        AirHockeyPuck puck =
            other.GetComponentInParent<AirHockeyPuck>();

        if (puck == null)
            return;

        if (gameManager != null)
            gameManager.RegisterGoal(pointForPlayer);
    }
}