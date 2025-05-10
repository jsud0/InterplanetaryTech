using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace BalloonModule
{
    public class BalloonModule : MonoBehaviour, Rocket.INJ_Location, Rocket.INJ_Physics, I_PartMenu
    {
        public Location Location { private get; set; }
        public Rigidbody2D Rb2d { get; set; }

        public Float_Reference radius;
        public double dragConstant = 1;
        public double areaToVolume = 20;
        public double buoyantMultiplier = 0.4;
        public double maxDeployVelocity;
        public Transform balloon;
        public OrientationModule orientation;

        [Space]
        public Float_Reference state;
        public Float_Reference targetState;
        public Float_Reference maxSpeed;

        private Double2 oldPosition;

        [Space]
        public UnityEvent onDeploy;

        void I_PartMenu.Draw(StatsMenu drawer, PartDrawSettings settings)
        {
            // drawer.DrawStat(40, Loc.main.Info_Parachute_Max_Height.Inject(this.maxDeployHeight.ToDistanceString(true), "height"), null);
        }

        public void DeployBalloon(UsePartData data)
        {
            bool flag = false;
            double num = this.Location.planet.HasAtmospherePhysics ? this.Location.planet.data.atmospherePhysics.parachuteMultiplier : 1.0;
            if (this.targetState.Value == 0f && this.state.Value == 0f)
            {
                if (!this.Location.planet.HasAtmospherePhysics || this.Location.Height > this.Location.planet.AtmosphereHeightPhysics * 0.9)
                {
                    MsgDrawer.main.Log("Cannot inflate balloon in vacuum");
                }
                else if (this.Location.velocity.magnitude > this.maxDeployVelocity * num)
                {
                    MsgDrawer.main.Log(Loc.main.Msg_Cannot_Deploy_Parachute_While_Faster.Inject((this.maxDeployVelocity * num).ToVelocityString(false, false), "velocity"));
                }
                else
                {
                    MsgDrawer.main.Log("Balloon inflated");
                    this.targetState.Value = 2f;
                    this.onDeploy.Invoke();
                    flag = true;
                }
            }
            else if (this.targetState.Value == 2f && this.state.Value == 2f)
            {
                MsgDrawer.main.Log("Balloon deflated");
                this.targetState.Value = 0f;
                this.state.Value = 0f;
                flag = true;
            }
            else if (this.targetState.Value == 3f)
            {
                flag = true;
            }
            if (!flag)
            {
                data.successfullyUsedPart = false;
            }
        }

        private void Start()
        {
            if (GameManager.main == null)
            {
                base.enabled = false;
                return;
            }
            this.targetState.OnChange += this.UpdateEnabled;
        }

        private void UpdateEnabled()
        {
            base.enabled = (this.targetState.Value == 1f || this.targetState.Value == 2f);
        }

        private void FixedUpdate()
        {
            if (this.state.Value == 0f) return;
            double scaledRadius = radius.Value * (this.state.Value / 2);
            double height = this.Location.Height;
            double density = this.Location.planet.GetAtmosphericDensity(height);
            double gravity = this.Location.planet.GetGravity(this.Location.planet.Radius + height);

            double volume = areaToVolume * Math.PI * scaledRadius * scaledRadius;
            double buoyantMagnitude = density * volume * gravity * buoyantMultiplier;

            if (maxSpeed.Value > 0.1 && this.Location.velocity.Mag_MoreThan(0.1) && this.Location.VerticalVelocity > maxSpeed.Value)
            {
                buoyantMagnitude *= maxSpeed.Value * maxSpeed.Value / this.Location.velocity.sqrMagnitude;
            }

            // double area = 2 * Math.PI * scaledRadius * scaledRadius;
            // double dragMagnitude = dragConstant * density * area * this.Location.velocity.sqrMagnitude / 2f;

            // double forceMagnitude = buoyantMagnitude - dragMagnitude;

            Vector2 upDirection = this.Location.position.normalized;
            float upAngle = Mathf.Atan2(upDirection.y, upDirection.x);
            float rocketAngle = this.Rb2d.rotation * Mathf.Deg2Rad;
            float orientationAngle = orientation.orientation.Value.z + (orientation.orientation.Value.y < 0 ? 180f : 0);
            orientationAngle *= Mathf.Deg2Rad;
            Vector2 direction = new Vector2(
                (float)Math.Cos(upAngle - rocketAngle - orientationAngle),
                (float)Math.Sin(upAngle - rocketAngle - orientationAngle)
            );

            Vector2 force = transform.TransformVector(direction * (float)buoyantMagnitude);
            Vector2 relativePoint = this.Rb2d.GetRelativePoint(
                Transform_Utility.LocalToLocalPoint(base.transform, this.Rb2d, new Vector2(0, 3f))
            );
            this.Rb2d.AddForceAtPosition(force, relativePoint, ForceMode2D.Force);
        }

        private void LateUpdate()
        {
            if (GameManager.main == null || this.Location.planet == null)
            {
                return;
            }
            if (this.oldPosition.x == 0.0 && this.oldPosition.y == 0.0)
            {
                this.oldPosition = WorldView.ToGlobalPosition(base.transform.position) - this.Location.velocity;
                this.AngleToOldPosition();
                return;
            }
            this.AngleToOldPosition();
        }

        private void AngleToOldPosition()
        {
            Vector2 upDirection = this.Location.position.normalized;
            float upAngle = Mathf.Atan2(upDirection.y, upDirection.x);
            float rocketAngle = this.Rb2d.rotation * Mathf.Deg2Rad;
            float orientationAngle = orientation.orientation.Value.z + (orientation.orientation.Value.y < 0 ? 180f : 0);
            orientationAngle *= Mathf.Deg2Rad;
            float angle = upAngle - rocketAngle - orientationAngle;
            angle *= Mathf.Rad2Deg;
            angle = 90f + angle;

            /*
            Double2 @double = WorldView.ToGlobalPosition(base.transform.position);
            Double2 a = this.oldPosition - @double;
            if (a.Mag_MoreThan(10.0))
            {
                this.oldPosition = @double + a.normalized * 10.0;
            }
            Vector2 vector = this.balloon.parent.InverseTransformVector(a);
            float num = Mathf.Atan2(vector.y, vector.x) * 57.29578f;
            */

            this.balloon.localEulerAngles = new Vector3(
                0f,
                0f,
                angle + Mathf.Sin(Time.time) * 3f * this.balloon.parent.lossyScale.x * this.balloon.parent.lossyScale.y
            );
        }
    }
}
