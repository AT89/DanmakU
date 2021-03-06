// Copyright (c) 2015 James Liu
//	
// See the LISCENSE file for copying permission.

using UnityEngine;
using Vexe.Runtime.Types;

namespace Hourai.DanmakU.Collider {

    [AddComponentMenu("Hourai.DanmakU/Colliders/Constant Force Collider")]
    public class ConstantForceCollider : DanmakuCollider {

        //TODO Document

        [SerializeField, Show]
        private Vector2 force;

        /// <summary>
        /// Gets or sets the force applied to contacting Danmaku.
        /// </summary>
        /// <remarks>
        /// Since Danmaku have no mass. The force applied in in terms of a constant displacement (units per second).
        /// </remarks>
        /// <value>The force applied.</value>
        public Vector2 Force {
            get { return force; }
            set { force = value; }
        }

        #region implemented abstract members of DanmakuCollider

        /// <summary>
        /// Handles a Danmaku collision. Only ever called with Danmaku that pass the filter.
        /// </summary>
        /// <param name="danmaku">the danmaku that hit the collider.</param>
        /// <param name="info">additional information about the collision</param>
        protected override void DanmakuCollision(Danmaku danmaku,
                                                 RaycastHit2D info) {
            danmaku.Position += force * dt;
        }

        #endregion
    }

}