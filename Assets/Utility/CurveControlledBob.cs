using System;
using UnityEngine;


namespace UnityStandardAssets.Utility
{
    [Serializable]
    public class CurveControlledBob
    {
        public float HorizontalBobRange = 0.33f;
        public float VerticalBobRange = 0.33f;
        public AnimationCurve Bobcurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f),
                                                            new Keyframe(1f, 0f), new Keyframe(1.5f, -1f),
                                                            new Keyframe(2f, 0f)); // sin curve for head bob
        public float VerticaltoHorizontalRatio = 1f;

        private float m_CyclePositionX;
        private float m_CyclePositionY;
        private float m_BobBaseInterval;
        private float m_Time;

        CharacterController controller;

        Transform cameraTransform;
        float lerpTime;

        float xPos;
        float yPos;
        public void Setup(Camera camera,CharacterController controller, float bobBaseInterval)
        {
            m_BobBaseInterval = bobBaseInterval;
            cameraTransform = camera.transform;
            //m_OriginalCameraPosition = camera.transform.localPosition;
            this.controller = controller;
            // get the length of the curve in time
            m_Time = Bobcurve[Bobcurve.length - 1].time;
        }


        public Vector3 DoHeadBob(float speed)
        {
            //hmmmmm
            xPos = cameraTransform.localPosition.x + (Bobcurve.Evaluate(m_CyclePositionX)*HorizontalBobRange);
            yPos = controller.center.y + controller.height / 2-0.1f + (Bobcurve.Evaluate(m_CyclePositionY)*VerticalBobRange);

            m_CyclePositionX += (speed*Time.deltaTime)/m_BobBaseInterval;
            m_CyclePositionY += ((speed*Time.deltaTime)/m_BobBaseInterval)*VerticaltoHorizontalRatio;

            if (m_CyclePositionX > m_Time)
            {
                m_CyclePositionX = m_CyclePositionX - m_Time;
            }
            if (m_CyclePositionY > m_Time)
            {
                m_CyclePositionY = m_CyclePositionY - m_Time;
            }

            return new Vector3(xPos, yPos, 0f);
        }
    }
}
