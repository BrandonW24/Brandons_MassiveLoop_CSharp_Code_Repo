using ML.SDK;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace EnemyAi
{

    public class EnemyAi : MonoBehaviour
    {

        public enum WayPoints { Arranged, Random };
        public WayPoints PatrolPoint;
        [HideInInspector]
        public Transform Target;

        NavMeshAgent agent;


        public string PlayerName;
        MLPlayer PlayerCharacter;

        private float Distance;

        private float Timer = 0.0f;
        public float FireRate;

        bool MoveToOtherPoint;

        Animator Anim;
        int ArrayNumber;
        bool Do_once;

        public Image HealthBarUI;
        private float DistanceToPlayer;
        public Transform Pos;
        public Transform Pos1;
        private bool CanISee;
        private bool CanIShoot;
        private bool Check;
        private float Suspicion;

        public Transform[] PatrolPoints;

        private bool LineOne;
        private bool LineTwo;

        /// <summary>
        /// ////
        /// </summary>
        public Transform TargetTransform;
        private Transform aimTransform;
        public Transform bone;

        private float angleLimit = 90.0f;
        //public float distanceLimit = 1.5f;


        public Transform MuzzleFlash_Postion;

        public GameObject MuzzleFlash;
        private float AttackTime;
        public float Damage;
        public float Loudness = 1.0f;
        bool Ready;
        bool ReadyToHear;

        private float randomDistance;
        private float NavSpeed;

        public Image notSign;
        private TrailRenderer BulletTrail;

        public AudioClip ShootingSFX;
        AudioSource audioSource;

        public int Ammo;
        private int CurrentAmmo;

        private bool CanIReload;
        private float ReloadTime;

        public AudioClip[] VoiceActing_Clips;

        public float Health = 100.0f;
        bool Death = false;
        bool IHeardSmothing = false;

        float IHeardSmothingTime = 0.0f;
        float DistanceToStone;
        Vector3 Vector3ToStone;

        public GameObject Bullet;
        float bulletForce = 20.0f;

        ///////////// Destroy State //////////////////////
        public enum DestroyED { Enable, Disable };
        public DestroyED DestroyState;
        [HideInInspector]
        public int NumberDestroy;
        float RandomDestroy;

        public Material ArmMaterial;
        public Material BodyMaterial;
        public Material LegMaterial;

        private MaterialPropertyBlock propertyBlock;
        private float dissolveValue = 0f; // Tracks the dissolve progress
        private bool isDying = false; // Tracks whether the robot is dissolving
        public float dissolveSpeed = 0.1f; // Speed at which the dissolve happens

        public GameObject DamageNumber;

        public GameObject HeadObject;
        public GameObject BloodVFX;

        private bool hasbeenHeadshot = false;
        private IEnumerator WaitForSeconds(float duration)
        {
            yield return new WaitForSeconds(duration);
        }

        private IEnumerator WaitForInitialization()
        {
            yield return new WaitForSeconds(4);
            MLPlayer nearestPlayer = MassiveLoopRoom.FindPlayerCloseToPosition(this.gameObject.transform.position);
            AssignPlayerCharacter(nearestPlayer);
        }


        public void RandomizeRobotColors()
        {
            // Define a list of colors for randomization
            Color[] colors = { Color.red, Color.yellow, Color.green, Color.blue, Color.white, Color.gray };

            // Select a random color from the list
            Color randomColor = colors[Random.Range(0, colors.Length)];

            // Retrieve the materials
            if (ArmMaterial != null)
            {
                ArmMaterial.color = randomColor;
            }
            else
            {
                Debug.LogError("ArmMaterial is not assigned!");
            }

            if (BodyMaterial != null)
            {
                BodyMaterial.color = randomColor;
            }
            else
            {
                Debug.LogError("BodyMaterial is not assigned!");
            }

            if (LegMaterial != null)
            {
                LegMaterial.color = randomColor;
            }
            else
            {
                Debug.LogError("LegMaterial is not assigned!");
            }

            Debug.Log($"Robot materials updated with color: {randomColor}");
        }

        // Callback for when a player is instantiated
        private void OnPlayerInstantiated(ML.SDK.MLPlayer player)
        {
            //   Debug.Log($"Player instantiated: {player.NickName}");

            if (player.IsInstantiated)
            {
                MLPlayer nearestPlayer = MassiveLoopRoom.FindPlayerCloseToPosition(this.gameObject.transform.position);
                AssignPlayerCharacter(nearestPlayer);
                TargetTransform = PlayerCharacter.PlayerRoot.transform;
                aimTransform = Pos;
                PlayerName = PlayerCharacter.NickName;
            }
        }

        // Assign the player character reference
        private void AssignPlayerCharacter(MLPlayer player)
        {
            PlayerCharacter = player;
            Debug.Log($"PlayerCharacter assigned: {PlayerCharacter.NickName}");
            TargetTransform = PlayerCharacter.PlayerRoot.transform;
            aimTransform = Pos;
            PlayerName = PlayerCharacter.NickName;
            // Unsubscribe from the event to avoid redundant calls


            // Additional initialization logic if needed
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("Begin ai");
            Anim = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
            Debug.Log("Retrieving PatrolPoints");

            if (PatrolPoints == null || PatrolPoints.Length == 0)
            {
                Debug.LogError("PatrolPoints is null or empty. Ensure patrol points are assigned in the spawner.");
                return;
            }
            Target = PatrolPoints[Random.Range(0, PatrolPoints.Length)];

            propertyBlock = new MaterialPropertyBlock();

            //   Pos = transform.Find("Pos").GetComponent<Transform>();
            //   Pos1 = transform.Find("Pos1").GetComponent<Transform>();

            StartCoroutine(WaitForInitialization());

            MLPlayer nearestPlayer = MassiveLoopRoom.FindPlayerCloseToPosition(this.gameObject.transform.position);
            AssignPlayerCharacter(nearestPlayer);

            Debug.Log("PatrolPoints Retrieved");

           // RandomizeRobotColors();

            //    MassiveLoopRoom.OnPlayerInstantiated += OnPlayerInstantiated;

            /*
            // If players are already instantiated when this script starts, handle it immediately
            MLPlayer nearestPlayer = MassiveLoopRoom.FindPlayerCloseToPosition(this.gameObject.transform.position);
            if (nearestPlayer != null)
            {
                AssignPlayerCharacter(nearestPlayer);
            }
            */
               MuzzleFlash_Postion = transform.Find("Skeleton/Hips/Spine/Chest/UpperChest/Right_Shoulder/Right_UpperArm/Right_LowerArm/Right_Hand/Weapons_Position/MuzzleFlash_Position").GetComponent<Transform>();

            //    HealthBarUI = transform.Find("Suspicion_Canvas/Full").GetComponent<Image>();
            //    notSign = transform.Find("Suspicion_Canvas/Not_Sign").GetComponent<Image>();

            notSign.gameObject.SetActive(false);
            randomDistance = Random.Range(3.0f, 10.0f);
            //   LineRenderer BulletTrail = GetComponent<LineRenderer>();
            audioSource = GetComponent<AudioSource>();
            CurrentAmmo = Ammo;

            //   BulletTrail = transform.Find("Skeleton/Hips/Spine/Chest/UpperChest/Right_Shoulder/Right_UpperArm/Right_LowerArm/Right_Hand/Weapons_Position/MuzzleFlash_Position/BulletTrace").GetComponent<TrailRenderer>();

            //  bone = transform.Find("Skeleton/Hips/Spine/Chest").GetComponent<Transform>();

            RandomDestroy = Random.Range(8.0f, 12.0f);
            ////////////////////////////// switch (HealthBarState) //////////////////////////////
            switch (DestroyState)

            {
                case DestroyED.Enable:
                    {

                        NumberDestroy = 0;
                        break;
                    }

                case DestroyED.Disable:
                    {

                        NumberDestroy = 1;
                        break;
                    }

            }


        }

        private void ApplyDissolveValue(float dissolveValue)
        {
            // Find all renderers on this robot (assumes the robot has multiple parts)
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                // Set the dissolve amount using the MaterialPropertyBlock
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_DissolveAmount", dissolveValue);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }


        // Update is called once per frame
        void Update()
        {
            if (PlayerCharacter == null) return;

            if (IHeardSmothingTime > 0.0f)
            {
                IHeardSmothingTime -= Time.deltaTime;

            }

            if (IHeardSmothingTime <= 0.0f)
            {
                IHeardSmothing = false;
                notSign.gameObject.SetActive(false);

            }


            DistanceToStone = Vector3.Distance(Vector3ToStone, transform.position);

            HealthBarUI.fillAmount = Suspicion / 100;

            ///////////////////// Attack //////////////////////////////////////


            if (DistanceToPlayer < 15.0f && Check == false)
            {
                Anim.SetBool("Idle_2", false);
            }

            if (DistanceToPlayer >= 15.0f && Check == false)
            {
                Anim.SetBool("Idle_2", true);
            }


            if (Health <= 0.0f)
            {
                if (hasbeenHeadshot)
                {
                    HeadObject.transform.localScale = Vector3.zero;
                    BloodVFX.SetActive(true);
                }

                agent.speed = 0.0f;
                Death = true;

               // dissolveValue = 0f;
                dissolveValue += Time.deltaTime * dissolveSpeed;
                dissolveValue = Mathf.Clamp01(dissolveValue); // Ensure it stays between 0 and 1
                ApplyDissolveValue(dissolveValue);


                this.gameObject.GetComponent<CapsuleCollider>().enabled = false;

                Anim.SetInteger("DeathRandom", Random.Range(0, 3));
                Anim.SetTrigger("Death");

                Anim.enabled = false;

                HealthBarUI.gameObject.SetActive(false);
                notSign.gameObject.SetActive(false);
                transform.GetComponent<CapsuleCollider>().enabled = false;

                DeathFunction();

            }


            if (LineOne == true)
            {

                CanISee = true;
                CanIShoot = true;

            }

            if (LineOne == false)
            {

                CanISee = false;
                CanIShoot = false;

            }


            if (Check == true && Death == false)
            {

                AttackTime -= Time.deltaTime;
                //  Debug.Log("Check evaluated true, Death evaluated false, attempting next attack if statement"); this is ok
            //    Debug.Log($"Attack time evaluated <= 0.0, Distance to player : {DistanceToPlayer}, CanIShoot : {CanIShoot},  CanIReload : {CanIReload}, CurrentAmmo : {CurrentAmmo}");
                if (AttackTime <= 0.0f && DistanceToPlayer <= 20.0f && CanIShoot && !CanIReload && CurrentAmmo > 0)
                {
                    /*
                    Debug.Log($"Attack time evaluated <= 0.0, Distance to player : {DistanceToPlayer}, CanIShoot : {CanIShoot},  CanIReload : {CanIReload}, CurrentAmmo : {CurrentAmmo}");
                    if (AttackTime <= 0.0f)
                    {
                        Debug.Log("AttackTime reached zero");
                    }
                    if (DistanceToPlayer <= 20.0f)
                    {
                        Debug.Log($"Distance to player: {DistanceToPlayer}");
                    }
                    if (CanIShoot)
                    {
                        Debug.Log("Enemy can shoot");
                    }
                    if (!CanIReload)
                    {
                        Debug.Log("Enemy is not reloading");
                    }
                    if (CurrentAmmo > 0)
                    {
                        Debug.Log($"Current ammo: {CurrentAmmo}");
                    }
                    */

                    AttackTime = FireRate;
                    CurrentAmmo--;

                    // Play attack effects
                    if (MuzzleFlash != null)
                    {
                        Object.Instantiate(MuzzleFlash, MuzzleFlash_Postion.position, MuzzleFlash_Postion.rotation);
                    }
                    if (ShootingSFX != null)
                    {
                        audioSource.PlayOneShot(ShootingSFX, 0.7f);
                    }
                    if (BulletTrail != null)
                    {
                        BulletTrail.enabled = true;
                        BulletTrail.SetPosition(0, MuzzleFlash_Postion.transform.position);
                        BulletTrail.SetPosition(1, MuzzleFlash_Postion.transform.position + MuzzleFlash_Postion.forward * -1000);
                    }

                    // Call shoot logic
                    shoot();
                    EventAttack();
                }
                if (AttackTime > 0.5f)
                {
                      BulletTrail.enabled = false;
                }




            }



            /////////////////////// Read to Hearing ///////////////////////////////////
            /*
            MakeNoise EnemyNoise = PlayerCharacter.GetComponent<MakeNoise>();
            if (EnemyNoise != null)
            {
                if (Ready == true && EnemyNoise.Noise == true)
                {
                    Check = true;
                }
            }
            */


            ////////// call all funtions ///////////
            if (Death == false)

            {

                LookAtTarget();
                LookAtNotSing();
                SeePlayer();
            //    SeePlayerTwo();
            //    HearingRange();
            }
            ////////////////  Distance btween target and enemy
            DistanceToPlayer = Vector3.Distance(PlayerCharacter.PlayerRoot.transform.position, transform.position);

            ////////////////  Angle or SightSystem /////////////////////////////////

            Vector3 targetDir = PlayerCharacter.PlayerRoot.transform.position - transform.position;
            float angle = Vector3.Angle(targetDir, transform.forward);

            if (angle < 90.0f && DistanceToPlayer < 15.0f && CanISee == true)
            {

                if (Suspicion <= 100)
                {
                    Suspicion += 0.2f;
                }
            }

            if (angle < 90.0f && DistanceToPlayer < 10.0f && CanISee == true && Check == false)
            {

                if (Suspicion <= 100)
                {
                    Suspicion += 2.7f;
                }
            }

            if (angle >= 90.0f || DistanceToPlayer > 15.0f || CanISee == false)
            {
                if (Suspicion >= 0)
                {
                    Suspicion -= 0.2f;

                }
            }

            ///////// active Suspicion //////////////////////////

            if (Suspicion <= 0.0f || Check == true && Death == false)
            {

                HealthBarUI.gameObject.SetActive(false);

            }
            if (Suspicion > 0.0f && Check == false && Death == false)
            {

                HealthBarUI.gameObject.SetActive(true);
                notSign.gameObject.SetActive(false);
            }
            ///////////////////////////////////////////////////////

            if (Suspicion >= 100)
            {
                Check = true;
                notSign.gameObject.SetActive(false);
            }


            if (DistanceToPlayer < 1.0f)
            {
                Check = true;
                Debug.Log($"AI is very close to player Check bool : {Check}");
            }


            // if(Timer > 0.0f) {  }

            if (Timer <= 0.0f)
            {
                /*
                Vector3 lTargetDir = Target.position - transform.position;
                lTargetDir.y = 0.0f;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lTargetDir), Time.time * 1.0f);
                */
                if (PatrolPoint == WayPoints.Random)
                {
                    Target = PatrolPoints[Random.Range(0, PatrolPoints.Length)];
                }

                if (PatrolPoint == WayPoints.Arranged)
                {

                    Target = PatrolPoints[ArrayNumber];
                }

                Timer = 1.0f;
            }



            if (IHeardSmothing == false)
            {
                agent.SetDestination(Target.position);

            }

            if (IHeardSmothing == true)
            {
                agent.SetDestination(Vector3ToStone);
            }

            if (Check == true && Death == false)
            {

                Anim.SetBool("Check", true);
                agent.SetDestination(PlayerCharacter.PlayerRoot.transform.position);
                //  

                if (DistanceToPlayer >= randomDistance || CanISee == false)
                {
                    if (Death == false)
                    {

                        float[] numbers = { 6.5f, 3.2f, 0.0f };
                        int randomIndex = Random.Range(0, 3);
                        float randomFloatFromNumbers = numbers[randomIndex];

                        Anim.SetBool("Rifle_Walk", true);
                        agent.speed = 2.5f;
                        NavSpeed = 1.0f;
                    }
                }

                if (DistanceToPlayer < randomDistance && CanISee == true || CanIReload == true)
                {
                    if (Death == false)
                    {
                        Anim.SetBool("Rifle_Walk", false);
                        NavSpeed = 0.0f;


                        LookAtToPlayer();

                    }

                }


            }

            if (NavSpeed == 0.0f)
            {
                agent.speed = 0.0f;
            }

            if (Check == false)
            {
                ////////////////  Distance between target and enemy
                Distance = Vector3.Distance(Target.position, transform.position);

                if (Distance >= 0.5f)
                {
                    Do_once = true;
                }
                if (Distance < 0.5f)
                {
                    /////// increased one ///////////////
                    if (Do_once == true)
                    {
                        ArrayNumber += 1;
                    }
                    ////////////////////////////////////

                    if (ArrayNumber >= PatrolPoints.Length)
                    {
                        ArrayNumber = 0;
                    }
                    ///////////////////////////////
                    Timer -= 0.1f * Time.deltaTime;
                    Do_once = false;
                }

                if (Distance <= 0.3f || DistanceToStone <= 0.06f)
                {
                    agent.speed = 0.0f;
                }
                if (Distance <= 0.2f || DistanceToStone <= 0.2f)
                {

                    Anim.SetBool("Walk", false);
                }

                if (Distance > 0.2f && Death == false || DistanceToStone > 0.06f && Death == false)
                {
                    agent.speed = 1.5f;

                }

                if (Distance > 0.2f && DistanceToStone > 0.2f)
                {
                    if (Death == false)
                    {
                        Anim.SetBool("Walk", true);
                    }

                }


            }



            if (CurrentAmmo <= 0)
            {
                Reload();
            }




            /////////////////////// Read to Hearing ///////////////////////////////////
            /*
            MakeNoiseByPressingKey ZombieNoise = (MakeNoiseByPressingKey)PlayerCharacter.GetComponent(typeof(MakeNoiseByPressingKey));
            if (ZombieNoise != null)
            {
                if (ReadyToHear == true && ZombieNoise.Noise == true)
                {
                    Check = true;
                }
            }
            */


        }
        ////////////////////  //////////////////////////
        ///

        public void EventAttack()
        {

            RaycastHit Riflehit;
            float Dis = 1000f;
            Vector3 fromPosition = transform.position;
            Vector3 toPosition = new Vector3(PlayerCharacter.PlayerRoot.transform.position.x, PlayerCharacter.PlayerRoot.transform.position.y, PlayerCharacter.PlayerRoot.transform.position.z);
            Vector3 direction = toPosition - fromPosition;

          //  Debug.DrawRay(Pos.position, direction, Color.cyan);

            if (Physics.Raycast(Pos.position, direction, out Riflehit, Dis))
            {


                if (Riflehit.transform.gameObject.name == PlayerName)
                {
                    // Debug.Log("Hit");

                    Riflehit.transform.gameObject.SendMessage("PlayerDamage", Damage);


                      BulletTrail.SetPosition(1, PlayerCharacter.PlayerRoot.transform.position);

                    //  BulletTrail.SetPosition(1, MuzzleFlash_Postion.transform.position + MuzzleFlash_Postion.transform.forward * range);
                      BulletTrail.SetPosition(1, PlayerCharacter.PlayerRoot.transform.position);
                }







            }


        }



        void Reload()
        {
            if (ReloadTime > 0.0f)
            {
                Anim.SetBool("Reload", true);
                ReloadTime -= 0.1f;
                CanIReload = true;
            }

            if (ReloadTime <= 0.0f)
            {
                Anim.SetBool("Reload", false);
                CurrentAmmo = Ammo;
                CanIReload = false;
                ReloadTime = 14.0f;

            }

        }


        void shoot()
        {

            GameObject bullet = Instantiate(Bullet, MuzzleFlash_Postion.position, MuzzleFlash_Postion.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.AddForce(MuzzleFlash_Postion.up * bulletForce,ForceMode.Impulse);
            bullet.transform.LookAt(PlayerCharacter.PlayerRoot.transform.position);

        }

        /// <summary>
        /// ///////////////////////
        /// </summary>

        void SeePlayer()
        {
         //   Debug.Log("Seeking...");

            RaycastHit hit;
            float range = 1000f;
            Vector3 fromPosition = Pos.transform.position;
            Vector3 toPosition = new Vector3(PlayerCharacter.PlayerRoot.transform.position.x, PlayerCharacter.PlayerRoot.transform.position.y + 1, PlayerCharacter.PlayerRoot.transform.position.z);
            Vector3 direction = toPosition - fromPosition;

            Debug.DrawRay(Pos.position, direction, Color.cyan);

            if (Physics.Raycast(Pos.position, direction, out hit, range))
            {
             //   Debug.Log($"My raycast hit an object : {hit.transform.gameObject.name}");
                if (hit.transform.gameObject.name.Contains("PlayerRoot"))
                {
                    // If the hit GameObject has an MLPlayer component
                    LineOne = true;
               //     Debug.Log("I can see the player (Line One)");

                }
                else
                {
                    // If the hit GameObject does not have an MLPlayer component
                    LineOne = false;
                }
                if (hit.transform.gameObject.name.Contains("PlayerRoot"))
                {
                    // If the hit GameObject has an MLPlayer component
                    CanIShoot = true;
                 //   Debug.Log("I can see the player");
                }
                else
                {
                    // If the hit GameObject does not have an MLPlayer component
                    CanIShoot = false;
                }





            }



        }



        ///////////////*******************////////////////////////////////////

        void SeePlayerTwo()
        {


            RaycastHit hit;
            float range = 1000f;
            Vector3 fromPosition = Pos1.transform.position;
            Vector3 toPosition = new Vector3(PlayerCharacter.PlayerRoot.transform.position.x, PlayerCharacter.PlayerRoot.transform.position.y + 1, PlayerCharacter.PlayerRoot.transform.position.z);
            Vector3 direction = toPosition - fromPosition;

            Debug.DrawRay(Pos.position, direction, Color.red);

            if (Physics.Raycast(Pos1.position, direction, out hit, range))
            {

                if (hit.transform.gameObject.name == PlayerName)
                {
                    LineTwo = true;
                }
                if (hit.transform.gameObject.name != PlayerName)
                {
                    LineTwo = false;
                }
                /*
                if (hit.transform.gameObject.name == PlayerName)
                {
                    CanIShoot = true;
                }
                if (hit.transform.gameObject.name != PlayerName)
                {
                    CanIShoot = false;
                }
                */




            }



        }

        ////   LookAtTarget  ///
        ///

        void LookAtTarget()
        {

            var rotation = Quaternion.LookRotation(PlayerCharacter.PlayerRoot.transform.position - HealthBarUI.gameObject.transform.position);
            rotation.x = 0; //This is for limiting the rotation to the y axis. I needed this for my project so just
            rotation.z = 0;           //      delete or add the lines you need to have it behave the way you want.
            HealthBarUI.gameObject.transform.rotation = Quaternion.Slerp(HealthBarUI.gameObject.transform.rotation, rotation, Time.deltaTime * 100.0f);

        }



        void LookAtNotSing()
        {

            var rotation = Quaternion.LookRotation(PlayerCharacter.PlayerRoot.transform.position - notSign.gameObject.transform.position);
            rotation.x = 0; //This is for limiting the rotation to the y axis. I needed this for my project so just
            rotation.z = 0;           //      delete or add the lines you need to have it behave the way you want.
            notSign.gameObject.transform.rotation = Quaternion.Slerp(notSign.gameObject.transform.rotation, rotation, Time.deltaTime * 100.0f);

        }



        ////   LookAtTarget  ///
        ///

        void LookAtToPlayer()
        {

            var rotation = Quaternion.LookRotation(PlayerCharacter.PlayerRoot.transform.position - transform.position);
            rotation.x = 0; //This is for limiting the rotation to the y axis. I needed this for my project so just
            rotation.z = 0;           //      delete or add the lines you need to have it behave the way you want.
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 1500.0f);

        }


        /// <summary>
        /// //
        /// </summary>
        /// <returns></returns>

        Vector3 GetTargetPosition()
        {
            if (TargetTransform == null || aimTransform == null)
            {
                Debug.LogError("TargetTransform or aimTransform is not assigned!");
                return PlayerCharacter.PlayerRoot.transform.position;
                
            }

            Vector3 targetDirection = TargetTransform.position - aimTransform.position;
            Vector3 aimDirection = aimTransform.forward;

            if (targetDirection == Vector3.zero || aimDirection == Vector3.zero)
            {
                Debug.LogError("TargetDirection or AimDirection is zero!");
                return Vector3.zero;
            }

            float blendOut = 0.0f;
            float targetAngle = Vector3.Angle(targetDirection, aimDirection);

            if (targetAngle > angleLimit)
            {
                blendOut += (targetAngle - angleLimit) / 50.0f;
            }

            Vector3 direction = Vector3.Slerp(targetDirection, aimDirection, blendOut);
            return aimTransform.position + direction;
        }


        void LateUpdate()
        {
            if (PlayerCharacter == null) return;
            if (Check && !Death)
            {
                if (TargetTransform == null || aimTransform == null)
                {
                    Debug.LogError("TargetTransform or aimTransform is null in LateUpdate!");
                    TargetTransform = PlayerCharacter.PlayerRoot.transform;
                    aimTransform = Pos;
                    PlayerName = PlayerCharacter.NickName;
                }

                Vector3 targetPosition = GetTargetPosition();
             //   Debug.Log($"Target Position: {targetPosition}");

                AimAtTarget(bone, targetPosition);
            }
        }



        private void AimAtTarget(Transform bone, Vector3 targetPosition)

        {
            Vector3 aimDirction = aimTransform.forward;
            Vector3 targetDirction = targetPosition - aimTransform.position;
            Quaternion aimTowards = Quaternion.FromToRotation(aimDirction, targetDirction);
            bone.rotation = aimTowards * bone.rotation;


        }




        ///////////////////    ////////////////////////////////////////////////


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Loudness);

        }


        void HearingRange()
        {

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, Loudness);

            foreach (Collider hitCollider in hitColliders)
            {



                ///////////////////////***************///////////////////////////////////////////
                ///


                if (hitCollider.gameObject.name == PlayerName)
                {
                    //   Debug.Log(hitCollider.gameObject.name);
                    ReadyToHear = true;
                }




                ////////////////////***************/////////////////////////////////////////////


                MakeNoise MakeNoiseCOL = (MakeNoise) hitCollider.gameObject.GetComponent(typeof(MakeNoise));
                if (MakeNoiseCOL != null)
                {

                    if (MakeNoiseCOL.NoiseTime > 0.0f && Distance <= 0.5f)
                    {

                        MakeNoiseCOL.NoiseTime -= Time.deltaTime;

                    }

                    // target.position = MakeNoiseCOL.VectorTarget;
                    if (IHeardSmothing == true)
                    {
                        //    agent.destination = MakeNoiseCOL.VectorTarget;
                        Vector3ToStone = MakeNoiseCOL.VectorTarget;
                    }

                    //      agent.SetDestination(PlayerCharacter.position);
                    // Target.position = MakeNoiseCOL.VectorTarget;

                    if (MakeNoiseCOL.DoOnce == true && Check == false)
                    {
                        if (VoiceActing_Clips.Length > 0)
                        {
                            audioSource.PlayOneShot(VoiceActing_Clips[Random.Range(0, VoiceActing_Clips.Length)], 0.7f);
                        }
                    }




                    IHeardSmothing = true;
                    IHeardSmothingTime = 10.0f;


                    // Debug.Log(hitCollider.gameObject.name);
                    notSign.gameObject.SetActive(true);
                    // Target = MakeNoiseCOL.NoiseTarget;
                    if (MakeNoiseCOL.NoiseTime <= 0.0f)
                    {



                        if (PatrolPoint == WayPoints.Random)
                        {
                            Target = PatrolPoints[Random.Range(0, PatrolPoints.Length)];
                        }

                        if (PatrolPoint == WayPoints.Arranged)
                        {

                            Target = PatrolPoints[ArrayNumber];
                        }

                        if (Target == null)
                        {
                            Target = PatrolPoints[0];
                        }
                        //   Destroy(MakeNoiseCOL);
                        //    Destroy(MakeNoiseCOL.gameObject);

                    }
                    Ready = true;
                }

                if (MakeNoiseCOL == null)
                {

                    Ready = false;
                }

                if (Ready == true && MakeNoiseCOL.Noise == true)
                {
                    Check = true;
                }

                EnemyAi OtherEnemyAi = (EnemyAi)hitCollider.gameObject.GetComponent(typeof(EnemyAi));

                if (OtherEnemyAi != null)
                {

                    if (OtherEnemyAi.Check == true)
                    {

                        //   Debug.Log("IhearYou");
                        if (Suspicion <= 100)
                        {
                            Suspicion += 3.0f;
                        }


                        //   OtherEnemyAi.Check = true;

                    }


                }

            }



        }




        public void EnemyDamage(int Damage, string DamageType, bool isHeadShot)
        {
            // Instantiate the damage number prefab at the HealthBarUI position
            GameObject damageTextObject = Instantiate(DamageNumber, HealthBarUI.transform.position, Quaternion.identity);

            Debug.Log($"Damage type : {DamageType}");

            // Set the damage number text
            TextMeshPro damageText = damageTextObject.GetComponent<TextMeshPro>();
            if (damageText != null)
            {
                /* This is for the Enums but that can't be used yet.
                switch (DamageType)
                {
                    case ElementalType.Normal:
                        damageText.color = Color.white;
                        break;

                    case ElementalType.Fire:
                        damageText.color = Color.yellow;
                        break;

                    case ElementalType.Ice:
                        damageText.color = Color.cyan;
                        break;
                    case ElementalType.Lightning:
                        damageText.color = Color.HSVToRGB(0.75f, 1f, 1f); // Purple in HSV
                        break;
                    case ElementalType.Poison:
                        damageText.color = Color.green;
                        break;

                    default:
                        Debug.LogWarning($"Unhandled damage type: {DamageType}");
                        damageText.color = Color.white; // Default color fallback
                        break;
                }
                */

                switch (DamageType)
                {
                    case "Normal":
                        damageText.color = Color.white;
                        break;

                    case "Fire":
                        damageText.color = Color.yellow;
                        break;

                    case "Ice":
                        damageText.color = Color.cyan;
                        break;

                    case "Lightning":
                        damageText.color = Color.HSVToRGB(0.75f, 1f, 1f); // Purple in HSV
                        break;

                    case "Poison":
                        damageText.color = Color.green;
                        break;

                    default:
                        Debug.LogWarning($"Unhandled damage type: {DamageType}");
                        damageText.color = Color.white; // Default color fallback
                        break;
                }

                if (isHeadShot)
                {
                    damageText.color = Color.red; // Default color fallback
                    hasbeenHeadshot = true;

                }

                damageText.text = Damage.ToString();
                Debug.Log($"Damage displayed: {damageText.text}");
            }
            else
            {
                Debug.LogWarning("DamageNumber prefab does not have a TextMeshPro component.");
            }

            // Now apply the damage to the enemy
            Health -= Damage;
            Check = true;
            Debug.Log($"Damage dealt to enemy: {Damage}");
        }




        void DeathFunction()
        {


            if (NumberDestroy == 0)
            {
                StartCoroutine(TimeToDestroy());
            }


        }


        IEnumerator TimeToDestroy()

        {
            yield return new WaitForSeconds(RandomDestroy);
            Destroy(gameObject);

        }




    }

}
