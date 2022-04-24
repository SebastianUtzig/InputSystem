using System.ComponentModel;
using UnityEngine.InputSystem.Controls;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
using UnityEditor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if a circle gesture is recognized within continuous position changes.
    /// </summary>
    /// <remarks>
    /// The action is started automatically. 
    /// If the input is idle for longer than idleTimeout the action is automatically cancelled and started again.
    /// If the circle gesture is performed slower than gestureMaxDuration it will not be recognized.
    /// Since a perfect circle is hard to draw circleCloseTolerance should be set like default or higher.
    /// The parameter maxDeltaAngle controls the smoothness of recognized circles. E.g. a maxDeltaAngle of 90 still allows
    /// turns of 90 degrees (square).
    ///
    /// <example>
    /// <code>
    /// // Action that requires a circle gesture to be completed without corners of 90 degree or smaller:
    /// var action = new InputAction(binding: "&lt;Mouse&gt;/position", interactions: "Circle(maxDeltaAngle=89.0)");T
    /// </code>
    /// </example>
    /// </remarks>
    [DisplayName("Circle")]
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
    public class CircleInteraction : IInputInteraction<Vector2>
    {
        /// <summary>
        /// Timeout (in seconds) which cancels a gesture on inactive input.
        /// </summary>
        public float idleTimeout = 0.5f;

        /// <summary>
        /// Maximum period of time (in seconds) in which a circle gesture is detected.
        /// </summary>
        public float gestureMaxDuration = 2f;

        /// <summary>
        /// Tolerance distance in constrained space which allows a closed circle.
        /// </summary>
        public float circleCloseTolerance = 0.075f;

        /// <summary>
        /// Maximum angle (in degree) between two consecutive motions that does not cancel gesture recognition.
        /// The smaller the angle the smoother the circle movement must be.
        /// </summary>
        public float maxDeltaAngle = 89f;

        private float m_inputIdleTimeout => idleTimeout;
        private float m_gestureMaxDuration => gestureMaxDuration;
        private float m_circleCloseTolerance => circleCloseTolerance;
        private float m_maxDeltaAngle => maxDeltaAngle;

        private Vector2 m_lastPosition = Vector2.zero;
        private bool m_lastAnglePositive = true;
        private float m_lastUpdate = Time.time;
        private Vector2 m_lastDirection = Vector2.zero;
        private bool m_isConstrainedSpace = true;

        private class CircleCandidate
        {
            public Vector2 Position { get; private set; }
            public float StartTime { get; private set; }
            public float AngleSum { get; set; }

            public CircleCandidate(Vector2 position, float time, float angle)
            {
                Position = position;
                StartTime = time;
                AngleSum = angle;
            }
        }

        private List<CircleCandidate> m_circleCandidates = new List<CircleCandidate>();

        static CircleInteraction()
        {
            InputSystem.RegisterInteraction<CircleInteraction>();
        }

        /// <inheritdoc />
        public void Process(ref InputInteractionContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Waiting:
                    // For now the gesture detection is always immediately started:
                    context.Started();
                    break;

                case InputActionPhase.Started:

                    Vector2 value = context.action.ReadValue<Vector2>();
                    if(value != null){

                        // Init / starting point:
                        if (m_lastPosition == Vector2.zero)
                        {
                            m_lastPosition = value;
                            break;
                        }

                        // Check for normalization:
                        if (value.x > 1 || value.y > 1)
                            m_isConstrainedSpace = false;

                        // Normalization:
                        Vector2 value_normalized = value;
                        Vector2 last_value_normalized = m_lastPosition;
                        if (!m_isConstrainedSpace)
                        {
                            value_normalized.x = ToConstrainedSpace(value.x, Screen.width);
                            value_normalized.y = ToConstrainedSpace(value.y, Screen.height);
                            last_value_normalized.x = ToConstrainedSpace(m_lastPosition.x, Screen.width);
                            last_value_normalized.y = ToConstrainedSpace(m_lastPosition.y, Screen.height);
                        }

                        Vector2 direction = last_value_normalized - value_normalized;
                        float distance = direction.magnitude;
                        // Only measure at a minimum distance to last sample:
                        if(distance > 0.02f){

                            //if (Camera.main != null)
                            //    Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(m_lastPosition.x, m_lastPosition.y, Camera.main.nearClipPlane)),
                            //        Camera.main.ScreenToWorldPoint(new Vector3(value.x, value.y, Camera.main.nearClipPlane)),
                            //        Random.ColorHSV(), m_gestureMaxDuration, false);


                            if (m_lastDirection != Vector2.zero){
                                float angle = Vector2.SignedAngle(m_lastDirection.normalized, direction.normalized);
                                bool angle_positive = angle >= 0;

                                // Gesture timeout due to inactivity:
                                if (Time.time - m_lastUpdate > m_inputIdleTimeout)
                                {
                                    Reset();
                                    context.Canceled();
                                    context.Started();
                                }

                                // Gesture interruption due to rough input
                                if (Mathf.Abs(angle) > m_maxDeltaAngle)
                                {
                                    Reset();
                                    context.Canceled();
                                    context.Started();
                                }

                                // Gesture timeout due to speed: (keep other circle candidates until maximum duration is reached)
                                while (m_circleCandidates.Count != 0 && ((Time.time - m_circleCandidates[0].StartTime) > m_gestureMaxDuration))
                                {
                                    m_circleCandidates.RemoveAt(0);
                                }


                                // Add new circle candidate when direction changes - keep old ones for tolerance
                                if (m_circleCandidates.Count == 0 || (angle_positive != m_lastAnglePositive))
                                {
                                    m_circleCandidates.Add(new CircleCandidate(last_value_normalized,Time.time,0));
                                }

                                // Apply angle to all circle candidates and check candidates for completes circle gestures:
                                bool found_circle = false;
                                for(int i = m_circleCandidates.Count-1; i >=0; --i)
                                {
                                    // Add up angle:
                                    m_circleCandidates[i].AngleSum += angle;

                                    // Since the closing edge is not included there is a tolerance of 15 degrees:
                                    bool is_potential_circle = (m_circleCandidates[i].AngleSum > 345) || (m_circleCandidates[i].AngleSum < -345);
                                    if (!is_potential_circle)
                                        continue;

                                    // Check for closed circle gestures - avoid spiral/helix being detected:
                                    var d = Vector2.Distance(value_normalized, m_circleCandidates[i].Position);
                                    if (d < m_circleCloseTolerance)
                                    {
                                        // Use closest circle candidate for gesture validation:
                                        found_circle = true;
                                        break;
                                    }
                                }

                                // Success:
                                if (found_circle)
                                {
                                    context.Performed();
                                    Reset();
                                    m_lastPosition = Vector2.zero;
                                    m_lastDirection = Vector2.zero;
                                    m_lastUpdate = Time.time;
                                    break;
                                }

                                m_lastAnglePositive = angle_positive;
                            }

                            m_lastPosition = value;
                            m_lastDirection = direction;
                            m_lastUpdate = Time.time;
                        }
                    }

                    break;

                case InputActionPhase.Performed:
                    break;
            }
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_circleCandidates.Clear();
        }

        private float ToConstrainedSpace (float number, float factor)
        {
            return ((number / factor) *2) -1; // -1 .. 1
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// UI that is displayed when editing <see cref="CircleInteraction"/> in the editor.
    /// </summary>
    internal class CircleInteractionEditor : InputParameterEditor<CircleInteraction>
    {
        protected override void OnEnable()
        {
            m_inputIdleTimeoutSetting.Initialize("Input Idle Timeout",
                "Timeout (in seconds) which cancels a gesture on inactive input.",
                "Default Input Idle Timeout",
                () => target.idleTimeout, v => target.idleTimeout = v, () => 0.5f, false); // TODO: add to input settings

            m_gestureMaxDuration.Initialize("Gesture Maximum Duration",
                "Maximum period of time (in seconds) in which a circle gesture is detected.",
                "Default Gesture Maximum Duration",
                () => target.gestureMaxDuration, v => target.gestureMaxDuration = v, () => 2f, false); // TODO: add to input settings

            m_circleCloseTolerance.Initialize("Circle Close Tolerance",
                "Tolerance distance in constrained space which allows a closed circle.",
                "Default Circle Close Tolerance",
                () => target.circleCloseTolerance, v => target.circleCloseTolerance = v, () => 0.075f, false); // TODO: add to input settings

            m_maxDeltaAngle.Initialize("Maximum Delta Angle",
                "Maximum angle (in degree) between two consecutive motions that does not cancel circle gesture recognition. The smaller the angle the smoother the circle movement must be.",
                "Default Maximum Delta Angle",
                () => target.maxDeltaAngle, v => target.maxDeltaAngle = v, () => 89f, false); // TODO: add to input settings
        }

        public override void OnGUI()
        {
            m_inputIdleTimeoutSetting.OnGUI();
            m_gestureMaxDuration.OnGUI();
            m_circleCloseTolerance.OnGUI();
            m_maxDeltaAngle.OnGUI();
        }

        private CustomOrDefaultSetting m_inputIdleTimeoutSetting;
        private CustomOrDefaultSetting m_gestureMaxDuration;
        private CustomOrDefaultSetting m_circleCloseTolerance;
        private CustomOrDefaultSetting m_maxDeltaAngle;
    }
    #endif
}
