using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretDemoSceneController : MonoBehaviour
{
    public Transform target;

    public List<VattalusWeaponController> listOfDemoWeapons = new List<VattalusWeaponController>();
    public List<VattalusTurretController> RandomMovementTurrets = new List<VattalusTurretController>();
    public List<VattalusTurretController> TargetTrackingTurrets = new List<VattalusTurretController>();


    void Start()
    {
        foreach (VattalusWeaponController weapon in listOfDemoWeapons)
        {
            StartCoroutine(PeriodicWeaponFire(weapon));
        }

        foreach (VattalusTurretController turret in RandomMovementTurrets)
        {
            StartCoroutine(PeriodicTurretMovement(turret));
        }

        if (target != null)
        {
            foreach (VattalusTurretController turret in TargetTrackingTurrets)
            {
                turret.SetTurretTarget(target, true);
            }
        }
    }

    IEnumerator PeriodicWeaponFire(VattalusWeaponController weapon)
    {
        if (weapon == null) yield break;

        while (Application.isPlaying)
        {
            if (weapon.firingPattern == VattalusWeaponController.VattalusFiringPattern.Gatling)
            {
                weapon.StartContinousFire();
                yield return new WaitForSeconds(weapon.cooldown + 1f * 10f);
                weapon.StopContinousFire();
                yield return new WaitForSeconds(weapon.cooldown + 1f * 10f);
            }
            else
            {
                weapon.FireWeapon(target);
            }

            yield return new WaitForSeconds(weapon.cooldown + UnityEngine.Random.Range(0.5f, 1.5f) * 6f);
        }
    }

    IEnumerator PeriodicTurretMovement(VattalusTurretController turret)
    {
        if (turret == null) yield break;

        while (Application.isPlaying)
        {
            turret.SetTurretAim(Random.Range(-180f, 180f), Random.Range(turret.ElevationAngleConstraints.x, turret.ElevationAngleConstraints.y), true);

            yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 8f));
        }

    }
}
