﻿using FarseerPhysics;
using Microsoft.Xna.Framework;
using System.Xml.Linq;
using System.Collections.Generic;
using System;

namespace Barotrauma
{
    abstract class AnimController : Ragdoll
    {
        public abstract GroundedMovementParams WalkParams { get; set; }
        public abstract GroundedMovementParams RunParams { get; set; }
        public abstract SwimParams SwimSlowParams { get; set; }
        public abstract SwimParams SwimFastParams { get; set; }

        public AnimationParams CurrentAnimationParams => (InWater || !CanWalk) ? (AnimationParams)CurrentSwimParams : CurrentGroundedParams;
        public GroundedMovementParams CurrentGroundedParams
        {
            get
            {
                if (!CanWalk)
                {
                    DebugConsole.ThrowError($"{character.SpeciesName} cannot walk!");
                    return null;
                }
                else
                {
                    return IsMovingFast ? RunParams : WalkParams;
                }
            }
        }
        public SwimParams CurrentSwimParams => IsMovingFast ? SwimFastParams : SwimSlowParams;

        public bool CanWalk => CanEnterSubmarine;
        // TODO: Presupposes that the slow speed is lower than the high speed. 
        // This is how it should be, but when the parameters are modified in the anim editor, it may be vice versa. 
        // How should we solve this? Restrict the slow speed value or refactor how the current params are handled?
        public bool IsMovingFast
        {
            get
            {
                if (InWater || !CanWalk)
                {
                    return TargetMovement.LengthSquared() > SwimSlowParams.Speed * SwimSlowParams.Speed;
                }
                else
                {
                    return Math.Abs(TargetMovement.X) > WalkParams.Speed;
                }
            }
        }

        /// <summary>
        /// Note: creates a new list every time, because the params might have changed. If there is a need to access the property frequently, change the implementation to an array, where the slot is updated when the param is updated(?)
        /// Currently it's not simple to implement, since the properties are not implemented here, but in the derived classes. Would require to change the params virtual and to call the base property getter/setter or something.
        /// </summary>
        public List<AnimationParams> AllAnimParams
        {
            get
            {
                if (CanWalk)
                {
                    return new List<AnimationParams> { WalkParams, RunParams, SwimSlowParams, SwimFastParams };
                }
                else
                {
                    return new List<AnimationParams> { SwimSlowParams, SwimFastParams };
                }
            }
        }

        public enum Animation { None, Climbing, UsingConstruction, Struggle, CPR };
        public Animation Anim;

        public Vector2 AimSourcePos => ConvertUnits.ToDisplayUnits(AimSourceSimPos);
        public virtual Vector2 AimSourceSimPos => Collider.SimPosition;

        protected float? GetValidOrNull(float v) => MathUtils.IsValid(v) ? new float?(v) : null;
        protected float? GetValidOrNull(AnimationParams p, float? v) => p == null ? null : GetValidOrNull(v.Value);

        protected override float? HeadPosition => GetValidOrNull(CurrentGroundedParams, CurrentGroundedParams?.HeadPosition);
        protected override float? TorsoPosition => GetValidOrNull(CurrentGroundedParams, CurrentGroundedParams?.TorsoPosition);
        protected override float? HeadAngle => GetValidOrNull(CurrentAnimationParams, CurrentAnimationParams?.HeadAngleInRadians);
        protected override float? TorsoAngle => GetValidOrNull(CurrentAnimationParams, CurrentAnimationParams?.TorsoAngleInRadians);

        protected float walkPos;

        public AnimController(Character character, string seed, RagdollParams ragdollParams = null) : base(character, seed, ragdollParams) { }

        public virtual void UpdateAnim(float deltaTime) { }

        public virtual void HoldItem(float deltaTime, Item item, Vector2[] handlePos, Vector2 holdPos, Vector2 aimPos, bool aim, float holdAngle) { }

        public virtual void DragCharacter(Character target) { }

        public virtual void UpdateUseItem(bool allowMovement, Vector2 handPos) { }

        public float GetSpeed(AnimationType type)
        {
            switch (type)
            {
                case AnimationType.Walk:
                    if (!CanWalk)
                    {
                        DebugConsole.ThrowError($"{character.SpeciesName} cannot walk!");
                        return 0;
                    }
                    return WalkParams.Speed;
                case AnimationType.Run:
                    if (!CanWalk)
                    {
                        DebugConsole.ThrowError($"{character.SpeciesName} cannot run!");
                        return 0;
                    }
                    return RunParams.Speed;
                case AnimationType.SwimSlow:
                    return SwimSlowParams.Speed;
                case AnimationType.SwimFast:
                    return SwimFastParams.Speed;
                default:
                    throw new NotImplementedException(type.ToString());
            }
        }

        public float GetCurrentSpeed(bool useMaxSpeed)
        {
            AnimationType animType;
            if (InWater || !CanWalk)
            {
                if (useMaxSpeed)
                {
                    animType = AnimationType.SwimFast;
                }
                else
                {
                    animType = AnimationType.SwimSlow;
                }
            }
            else
            {
                if (useMaxSpeed)
                {
                    animType = AnimationType.Run;
                }
                else
                {
                    animType = AnimationType.Walk;
                }
            }
            return GetSpeed(animType);
        }

        public AnimationParams GetAnimationParamsFromType(AnimationType type)
        {
            switch (type)
            {
                case AnimationType.Walk:
                    return WalkParams;
                case AnimationType.Run:
                    return RunParams;
                case AnimationType.SwimSlow:
                    return SwimSlowParams;
                case AnimationType.SwimFast:
                    return SwimFastParams;
                default:
                    throw new NotImplementedException(type.ToString());
            }
        }
    }
}
