using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

public class BumperController : NetworkBehaviour
{
    private float bumpForce = 10f;
    private Vector3 ogScale;
    Sequence bumpSequence;

    private void Awake()
    {
        ogScale = transform.localScale;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        ContactPoint contact = collision.GetContact(0);

        Vector3 bumpDirection = (contact.point - transform.position).normalized;

        collision.gameObject.GetComponent<PlayerController>().AddKnockback(bumpDirection * bumpForce, 0.8f);

        if (bumpSequence != null && bumpSequence.IsActive())
        {
            bumpSequence.Kill();
        }

        bumpSequence = DOTween.Sequence();
        bumpSequence.Append(transform.DOScale(ogScale * 1.2f, 0.15f).SetEase(Ease.OutElastic));
        bumpSequence.Append(transform.DOScale(ogScale, 0.3f).SetEase(Ease.OutElastic));
    }
}
