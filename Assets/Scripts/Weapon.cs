using UnityEngine;
using DG.Tweening;
using System.Collections;
using Unity.Netcode;
using System.Linq;

public class Weapon : MonoBehaviour
{
    private float recoilForce = 30f;
    private float cooldown = 1.3f;
    private Sequence shootAnimation;

    [SerializeField] GameObject projectile;
    [SerializeField] GameObject fakeProjectile;
    [SerializeField] Transform projectileSpawn;
    [SerializeField] Transform smokeSpawn;

    bool canShoot = true;

    private GameObject rightHand;
    private GameObject leftHand;

    public void PlayShootAnimation()
    {
        if (!canShoot) return;

        rightHand.transform.SetParent(transform);

        canShoot = false;

        fakeProjectile.SetActive(false);

        ParticleManager.instance.SpawnParticles("RocketShootingEffect", projectileSpawn.position, projectileSpawn.rotation);
        ParticleManager.instance.SpawnParticles("ShootingCloudBurst", smokeSpawn);


        ShootProjectile();

        shootAnimation = DOTween.Sequence();
        shootAnimation.Append(transform.DOLocalRotate(new Vector3(-60, 0, 0), 0.2f));
        shootAnimation.Join(transform.DOLocalMove(new Vector3(0, 0.5f, -1), 0.2f));
        shootAnimation.Append(transform.DOLocalRotate(Vector3.zero, 0.2f));
        shootAnimation.Join(transform.DOLocalMove(Vector3.zero, 0.2f));
        shootAnimation.AppendInterval(0.5f);
        shootAnimation.AppendCallback(() => PlayReloadAnimation());
        shootAnimation.AppendCallback(() => StartCoroutine(ShootingCooldown()));
    }

    public void ShootProjectile()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        GameObject newProjectile = Instantiate(projectile, projectileSpawn.position, projectileSpawn.rotation);
        newProjectile.GetComponent<NetworkObject>().Spawn();
    }

    public float GetRecoilForce()
    {
        return recoilForce;
    }

    public IEnumerator ShootingCooldown()
    {
        yield return new WaitForSeconds(cooldown);
        canShoot = true;
    }

    public bool GetCanShoot()
    {
        return canShoot;
    }

    public void PlayReloadAnimation()
    {
        ToggleFakeProjectile(leftHand.transform);

        Vector3 leftHandOgPos = leftHand.transform.localPosition;

        // Control de la posicion del proyectil falso
        Sequence reloadAnimationFakeProjectile = DOTween.Sequence();
        reloadAnimationFakeProjectile.AppendInterval(0.1f);
        reloadAnimationFakeProjectile.AppendCallback(() => fakeProjectile.SetActive(true));
        reloadAnimationFakeProjectile.AppendInterval(0.6f);
        reloadAnimationFakeProjectile.AppendCallback(() => ToggleFakeProjectile(projectileSpawn));

        // Control del movimiento en el eje Z de la mano
        Sequence reloadAnimationZ = DOTween.Sequence();
        reloadAnimationZ.Append(leftHand.transform.DOLocalMoveZ(leftHandOgPos.z - 1f, 0.2f));
        reloadAnimationZ.Append(leftHand.transform.DOLocalMoveZ(projectileSpawn.localPosition.z + 1f, 0.3f).SetEase(Ease.InSine));
        reloadAnimationZ.Append(leftHand.transform.DOLocalMoveZ(projectileSpawn.localPosition.z, 0.2f));
        reloadAnimationZ.Append(leftHand.transform.DOLocalMoveZ(projectileSpawn.localPosition.z + 1f, 0.2f).SetEase(Ease.InSine));
        reloadAnimationZ.Append(leftHand.transform.DOLocalMoveZ(leftHandOgPos.z, 0.3f));

        // Control del movimiento en el eje X de la mano
        Sequence reloadAnimationX = DOTween.Sequence();
        reloadAnimationX.AppendInterval(0.2f);
        reloadAnimationX.Append(leftHand.transform.DOLocalMoveX(leftHandOgPos.x - 1f, 0.2f).SetEase(Ease.InSine));
        reloadAnimationX.Append(leftHand.transform.DOLocalMoveX(projectileSpawn.localPosition.x, 0.3f));
        reloadAnimationX.Append(leftHand.transform.DOLocalMoveX(leftHandOgPos.x - 1f, 0.3f).SetEase(Ease.InSine));
        reloadAnimationX.Append(leftHand.transform.DOLocalMoveX(leftHandOgPos.x, 0.2f));

        // Control del movimiento en el eje Y de la mano
        Sequence reloadAnimationY = DOTween.Sequence();
        reloadAnimationY.Append(leftHand.transform.DOLocalMoveY(projectileSpawn.localPosition.y, 0.7f));
        reloadAnimationY.Append(leftHand.transform.DOLocalMoveY(leftHandOgPos.y, 0.5f));
    }

    private void ToggleFakeProjectile(Transform newParent)
    {
        fakeProjectile.transform.SetParent(newParent);
        fakeProjectile.transform.position = newParent.position;
    }

    public void SetPlayerHands(GameObject rightHand, GameObject leftHand)
    {
        this.rightHand = rightHand;
        this.leftHand = leftHand;
    }
}
