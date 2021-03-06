using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Box2DSharp.Collision.Shapes;
using Box2DSharp.Common;
using Box2DSharp.Dynamics;
using Box2DSharp.Dynamics.Joints;
using NETCoreTest.Framework;

namespace NETCoreTest
{
    class Test
    {
        public readonly World World;

        private const int Count = 800;

        public int FrameCount = 2000;

        private RevoluteJoint _joint;

        private int _bodyCount;

        public Profile MaxProfile = new Profile();

        public Profile TotalProfile = new Profile();

        public FixedUpdate FixedUpdate;

        public Test()
        {
            World = new World(new Vector2(0, -9.8f));
        }

        public void Tumbler(bool stressTest = false, bool showProfile = false)
        {
            if (stressTest == false)
            {
                Console.Clear();
            }

            Body ground;
            {
                var bd = new BodyDef();
                ground = World.CreateBody(bd);
            }

            {
                var bd = new BodyDef
                {
                    BodyType = BodyType.DynamicBody,
                    AllowSleep = false,
                    Position = new Vector2(0.0f, 10.0f)
                };
                var body = World.CreateBody(bd);

                var shape = new PolygonShape();
                shape.SetAsBox(0.5f, 10.0f, new Vector2(10.0f, 0.0f), 0.0f);
                body.CreateFixture(shape, 5.0f);
                shape.SetAsBox(0.5f, 10.0f, new Vector2(-10.0f, 0.0f), 0.0f);
                body.CreateFixture(shape, 5.0f);
                shape.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, 10.0f), 0.0f);
                body.CreateFixture(shape, 5.0f);
                shape.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, -10.0f), 0.0f);
                body.CreateFixture(shape, 5.0f);

                var jd = new RevoluteJointDef
                {
                    BodyA = ground,
                    BodyB = body,
                    LocalAnchorA = new Vector2(0.0f, 10.0f),
                    LocalAnchorB = new Vector2(0.0f, 0.0f),
                    ReferenceAngle = 0.0f,
                    MotorSpeed = 0.05f * Settings.Pi,
                    MaxMotorTorque = 1e8f,
                    EnableMotor = true
                };
                _joint = (RevoluteJoint) World.CreateJoint(jd);
            }

            _bodyCount = 0;
            if (stressTest)
            {
                var timer = Stopwatch.StartNew();
                for (int i = 0; i < FrameCount; i++)
                {
                    Step(false);
                }

                timer.Stop();
                Console.WriteLine($"{timer.ElapsedMilliseconds} ms");
            }
            else
            {
                FixedUpdate = new FixedUpdate {UpdateCallback = () => Step(showProfile)};
                while (true)
                {
                    FixedUpdate.Tick();
                }
            }
        }

        private const float Dt = 1 / 60f;

        private readonly StringBuilder _sb = new StringBuilder();

        private void Step(bool showProfile)
        {
            World.Step(Dt, 8, 3);
            if (_bodyCount < Count)
            {
                var bd = new BodyDef
                {
                    BodyType = BodyType.DynamicBody,
                    Position = new Vector2(0.0f, 10.0f)
                };
                var body = World.CreateBody(bd);

                var shape = new PolygonShape();
                shape.SetAsBox(0.125f, 0.125f);
                body.CreateFixture(shape, 1.0f);

                ++_bodyCount;
            }

            if (showProfile)
            {
                var p = World.Profile;

                // Track maximum profile times
                MaxProfile.Step = Math.Max(MaxProfile.Step, p.Step);
                MaxProfile.Collide = Math.Max(MaxProfile.Collide, p.Collide);
                MaxProfile.Solve = Math.Max(MaxProfile.Solve, p.Solve);
                MaxProfile.SolveInit = Math.Max(MaxProfile.SolveInit, p.SolveInit);
                MaxProfile.SolveVelocity = Math.Max(MaxProfile.SolveVelocity, p.SolveVelocity);
                MaxProfile.SolvePosition = Math.Max(MaxProfile.SolvePosition, p.SolvePosition);
                MaxProfile.SolveTOI = Math.Max(MaxProfile.SolveTOI, p.SolveTOI);
                MaxProfile.Broadphase = Math.Max(MaxProfile.Broadphase, p.Broadphase);

                TotalProfile.Step += p.Step;
                TotalProfile.Collide += p.Collide;
                TotalProfile.Solve += p.Solve;
                TotalProfile.SolveInit += p.SolveInit;
                TotalProfile.SolveVelocity += p.SolveVelocity;
                TotalProfile.SolvePosition += p.SolvePosition;
                TotalProfile.SolveTOI += p.SolveTOI;
                TotalProfile.Broadphase += p.Broadphase;

                var aveProfile = new Profile();
                if (FixedUpdate.UpdateTime.FrameCount > 0)
                {
                    var scale = 1.0f / FixedUpdate.UpdateTime.FrameCount;
                    aveProfile.Step = scale * TotalProfile.Step;
                    aveProfile.Collide = scale * TotalProfile.Collide;
                    aveProfile.Solve = scale * TotalProfile.Solve;
                    aveProfile.SolveInit = scale * TotalProfile.SolveInit;
                    aveProfile.SolveVelocity = scale * TotalProfile.SolveVelocity;
                    aveProfile.SolvePosition = scale * TotalProfile.SolvePosition;
                    aveProfile.SolveTOI = scale * TotalProfile.SolveTOI;
                    aveProfile.Broadphase = scale * TotalProfile.Broadphase;
                }

                _sb.AppendLine($"FPS {FixedUpdate.UpdateTime.FramePerSecond}, ms {FixedUpdate.UpdateTime.Elapsed.TotalMilliseconds}");
                _sb.AppendLine($"step [ave] (max) = {p.Step} [{aveProfile.Step}] ({MaxProfile.Step})");
                _sb.AppendLine($"collide [ave] (max) = {p.Collide} [{aveProfile.Collide}] ({MaxProfile.Collide})");
                _sb.AppendLine($"solve [ave] (max) = {p.Solve} [{aveProfile.Solve}] ({MaxProfile.Solve})");
                _sb.AppendLine($"solve init [ave] (max) = {p.SolveInit} [{aveProfile.SolveInit}] ({MaxProfile.SolveInit})");
                _sb.AppendLine($"solve velocity [ave] (max) = {p.SolveVelocity} [{aveProfile.SolveVelocity}] ({MaxProfile.SolveVelocity})");
                _sb.AppendLine($"solve position [ave] (max) = {p.SolvePosition} [{aveProfile.SolvePosition}] ({MaxProfile.SolvePosition})");
                _sb.AppendLine($"solveTOI [ave] (max) = {p.SolveTOI} [{aveProfile.SolveTOI}] ({MaxProfile.SolveTOI})");
                _sb.AppendLine($"broad-phase [ave] (max) = {p.Broadphase} [{aveProfile.Broadphase}] ({MaxProfile.Broadphase})");

                Console.SetCursorPosition(0, 0);
                Console.Write(_sb.ToString());
                _sb.Clear();
            }
        }
    }
}