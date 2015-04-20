// Copyright (c) 2015 James Liu
//	
// See the LISCENSE file for copying permission.

using UnityEngine;
using UnityUtilLib;
using UnityUtilLib.Pooling;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A development kit for quick development of 2D Danmaku games
/// </summary>
namespace DanmakU {

	public delegate IEnumerator DanmakuTask(Danmaku danmaku);

	/// <summary>
	/// A single projectile fired.
	/// The base object that represents a single bullet in a Danmaku game
	/// </summary>
	public sealed partial class Danmaku : IPooledObject, IPrefabed<DanmakuPrefab>, IDanmakuObject {
		
		internal int poolIndex;
		internal int renderIndex;

		//private Stack<Component> extraComponents;

		//private GameObject gameObject;
		//private Transform transform;
		//private SpriteRenderer renderer;

		//internal float rotation;
		internal Vector2 direction;

		//Cached information about the Danmaku from its prefab
		internal Vector2 colliderOffset = Vector2.zero; 
		private float colliderRadius = 1f;
		//internal Sprite sprite;
		//internal Material material;
		//internal Color color;
		internal string tag;
		internal int layer;
		internal int frames;
		internal float time;
		internal string cachedTag;
		internal int cachedLayer;
		internal bool symmetric;

		//Prefab information
		private DanmakuPrefab prefab;
		private DanmakuPrefab runtime;

		//Collision related variables
		private int colliderMask;

		private bool to_deactivate;
		
		private DanmakuController controllerUpdate;
		internal List<DanmakuGroup> groups;
		private Bounds2D bounds;

		//Preallocated variables to avoid allocation in Update
		private int count, count2;
		private float distance;
		private Vector2 originalPosition, movementVector;
//		private IDanmakuCollider[] scripts;
		private RaycastHit2D[] raycastHits;
		private Collider2D[] colliders;
		private Vector2 collisionCenter;
		private float movementChange;

		//Cached check for controllers to avoid needing to calculate them in Update
		internal bool groupCheck;
		private bool controllerCheck;
		internal int groupCountCache;

		private List<IEnumerator> tasks;

		public float Speed;
		public float AngularSpeed;

		public DanmakuPrefab Prefab {
			get {
				return runtime;
			}
		}

		/// <summary>
		/// Gets or sets the damage this projectile does to entities.
		/// Generally speaking, this is only used for projectiles fired by the player at enemies
		/// </summary>
		/// <value>The damage this projectile does.</value>
		public int Damage;
		
		/// <summary>
		/// Gets the renderer sprite of the projectile.
		/// <see href="http://docs.unity3d.com/ScriptReference/SpriteRenderer-sprite.html">SpriteRenderer.sprite</see>
		/// </summary>
		/// <value>The sprite.</value>
		public Sprite Sprite {
			get {
				//return sprite;
				return runtime.Sprite;
			}
			//set {
			//	sprite = value;
			//	renderer.sprite = value;
			//}
		}
		
		/// <summary>
		/// Gets or sets the renderer color of the projectile.
		/// <see href="http://docs.unity3d.com/ScriptReference/SpriteRenderer-color.html">SpriteRenderer.color</see>
		/// </summary>
		/// <value>The renderer color.</value>
		public Color32 Color;

		public Material Material {
			get {
				//return material;
				return runtime.Material;
			}
			//set {
			//	material = value;
			//	renderer.material = value;
			//}
		}
		
		/// <summary>
		/// Gets or sets the position, in world space, of the projectile.
		/// </summary>
		/// <value>The position of the projectile.</value>
		public Vector2 Position;

		internal float rotation;

		/// <summary>
		/// Gets or sets the rotation of the projectile, in degrees.
		/// If viewed from a unrotated orthographic camera:
		/// 0 - Straight up
		/// 90 - Straight Left
		/// 180 - Straight Down
		/// 270 -  Straight Right
		/// </summary>
		/// <value>The rotation of the bullet in degrees.</value>
		public float Rotation {
			get {
				//return rotation;
				return rotation;
			}
			set {
				//if(!symmetric)
				//	transform.localRotation = Quaternion.Euler(0f, 0f, value);
				//rotation = value;
				rotation = value;
				//direction = UnitCircle(rotation);
				direction = UnitCircle(value);
			}
		}
		
		/// <summary>
		/// Gets the direction vector the projectile is facing.
		/// It is a unit vector.
		/// Changing <see cref="Rotation"/> will change this vector.
		/// </summary>
		/// <value>The direction vector the projectile is facing toward.</value>
		public Vector2 Direction {
			get {
				return direction;
			}
		}

		public float Scale;

		/// <summary>
		/// The amount of time, in seconds,that has passed since this bullet has been fired.
		/// This is calculated based on the number of AbstractDanmakuControllerd frames that has passed since the bullet has fired
		/// Pausing will cause this value to stop increasing
		/// </summary>
		/// <value>The time since the projectile has been fired.</value>
		public float Time {
			get {
				return time;
			}
		}
		
		/// <summary>
		/// The number of frames that have passed since this bullet has been fired.
		/// </summary>
		/// <value>The frame count since this bullet has been fired.</value>
		public int Frames {
			get {
				return frames;
			}
		}
		
		/// <summary>
		/// Gets the projectile's tag.
		/// </summary>
		/// <value>The tag of the projectile.</value>
		public string Tag {
			get {
				return tag;
			}
			set {
				tag = value;
				//gameObject.tag = value;
			}
		}
		
		/// <summary>
		/// Gets the projectile's layer.
		/// </summary>
		/// <value>The layer of the projectile.</value>
		public int Layer {
			get {
				return layer;
			}
			set {
				layer = value;
				colliderMask = collisionMask[layer];
			}
		}

		public bool BoundsCheck;
		public bool CollisionCheck;

		#region IDanmakuObject implementation
		/// <summary>
		/// Gets the DanmakuField this instance was fired from.
		/// </summary>
		/// <value>The field the projectile was fired from.</value>
		public DanmakuField Field {
			get;
			set;
		}
		#endregion

		public void StartTask(IEnumerator task) {
			if (tasks == null)
				tasks = new List<IEnumerator> ();
			if (task != null)
				tasks.Add (task);
			else
				Debug.LogError ("Attempted to start a null task");
		}

		public void StartTask(DanmakuTask task) {
			if(tasks == null)
				tasks = new List<IEnumerator>();
			if (task != null) {
				IEnumerator newTask = task (this);
				if (newTask != null)
					tasks.Add (newTask);
				else
					Debug.LogError ("Attempted to start a null task");
			} else {
				Debug.LogError ("Attempted to start a null task");
			}
		}

		public void AddController(IDanmakuController controller) {
			if(controller != null) {
				controllerUpdate += controller.UpdateDanmaku;
				controllerCheck = controllerUpdate != null;
			}
		}

		public void AddController(DanmakuController controller) {
			controllerUpdate += controller;
			controllerCheck = controllerUpdate != null;
		}

		public void RemoveController(IDanmakuController controller) {
			if(controller != null) {
				controllerUpdate -= controller.UpdateDanmaku;
				controllerCheck = controllerUpdate != null;
			}
		}

		public void RemoveController(DanmakuController controller) {
			controllerUpdate -= controller;
			controllerCheck = controllerUpdate != null;
		}

//		public T AddComponent<T>() where T : Component {
//			T component = gameObject.AddComponent<T> ();
//			if (extraComponents == null)
//				extraComponents = new Stack<Component> ();
//			extraComponents.Push (component);
//			return component;
//		}

		public void ClearControllers() {
			controllerCheck = true;
			controllerUpdate = null;
		}

		public void Rotate(DynamicFloat delta) {
			//float Delta = delta.Value;
			//if(!symmetric)
			//	transform.Rotate(0f, 0f, Delta);
			Rotation += delta.Value;
			//direction = UnitCircle (rotation);
		}
		
		/// <summary>
		/// Compares the tag of the Danmaku instance to the given string.
		/// Mirrors <a href="http://docs.unity3d.com/ScriptReference/GameObject.CompareTag.html">GameObject.CompareTag</a>.
		/// </summary>
		/// <returns><c>true</c>, if tag is an exact match to the string, <c>false</c> otherwise.</returns>
		/// <param name="tag">Tag.</param>
//		public bool CompareTag(string tag) {
//			return gameObject.CompareTag (tag);
//		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DanmakU.Danmaku"/> class.
		/// </summary>
		internal Danmaku() {
			groups = new List<DanmakuGroup> ();

			//gameObject = new GameObject ();
			//transform = gameObject.transform;
			//renderer = gameObject.AddComponent<SpriteRenderer> ();
			#if UNITY_EDITOR
			//This is purely for cleaning up the inspector, no need in an actual build
			//gameObject.hideFlags = HideFlags.HideInHierarchy;
			#endif
			raycastHits = new RaycastHit2D[5];
			colliders = new Collider2D[5];
//			scripts = new IDanmakuCollider[5];
		}

		internal void Update() {

			#region thread_unsafe
			if (controllerCheck) {
				controllerUpdate(this, dt);
			}

			if(tasks != null) {
				count = tasks.Count;
				count2 = 0;
				while(count2 < count) {
					if(!tasks[count2].MoveNext())
						tasks.RemoveAt(count2);
					else
						count2++;
				}
			}

			#endregion

			#region thread_safe
			if(AngularSpeed != 0f) {
				Rotation += AngularSpeed * dt;
			}

			if (Speed != 0) {
				movementChange = Speed * dt;
				Position.x += direction.x * movementChange;
				Position.y += direction.y * movementChange;
			}
			
			movementVector.x = Position.x - originalPosition.x;
			movementVector.y = Position.y - originalPosition.y;

			//Debug.DrawRay(originalPosition, movementVector);

			#endregion
			if(CollisionCheck) {
				distance = movementVector.magnitude;
				collisionCenter.x = originalPosition.x + colliderOffset.x * direction.x;
				collisionCenter.y = originalPosition.y + colliderOffset.y * direction.y;
				//Check if the collision detection should be continuous or not
				if (distance <= colliderRadius) {
					count = Physics2D.OverlapCircleNonAlloc(collisionCenter,
					                                        colliderRadius,
					                                        colliders,
					                                        colliderMask);
					for (int i = 0; i < count; i++) {
//						GameObject go = colliders [i].gameObject;
//						scripts = Util.GetComponentsPrealloc (go, scripts, out count2);
//						for (int j = 0; j < count2; j++) {
						IDanmakuCollider[] scripts;
						Collider2D collider = colliders[i];
						if(collider == null)
							continue;
						if(Field.colliderMap.ContainsKey(collider))
							scripts = Field.colliderMap[collider];
						else
							scripts = Util.GetComponents<IDanmakuCollider>(collider.gameObject);
						for (int j = 0; j < scripts.Length; j++) {
							scripts [j].OnDanmakuCollision (this);
						}
						if (to_deactivate) {
							Position = Physics2D.CircleCast (collisionCenter, colliderRadius, movementVector, distance).point;
							Deactivate();
							break;
						}
					}
				} else {
					count = Physics2D.CircleCastNonAlloc(collisionCenter, 
					                                     colliderRadius,
					                                     movementVector,
					                                     raycastHits,
					                                     distance,
					                                     colliderMask);
					for (int i = 0; i < count; i++) {
						RaycastHit2D hit = raycastHits [i];
//						GameObject go = hit.collider.gameObject;
//						scripts = Util.GetComponentsPrealloc (go, scripts, out count2);
//						for (int j = 0; j < count2; j++) {
						
						IDanmakuCollider[] scripts;
						Collider2D collider = hit.collider;
						if(collider == null)
							continue;
						if(Field.colliderMap.ContainsKey(collider))
							scripts = Field.colliderMap[collider];
						else
							scripts = Util.GetComponents<IDanmakuCollider>(collider.gameObject);
						for (int j = 0; j < scripts.Length; j++) {
							scripts [j].OnDanmakuCollision (this);
						}
						if (to_deactivate) {
							Position = hit.point;
							DeactivateImmediate();
							break;
						}
					}
				}
			}

			if (!is_active || (BoundsCheck && !bounds.Contains (Position))) {
				DeactivateImmediate();
				return;
			}
			
			frames++;
			time += dt;
			originalPosition.x = Position.x;
			originalPosition.y = Position.y;
		}

		/// <summary>
		/// Makes the instance of Danmaku match the given ProjectilePrefab
		/// This copies:
		/// - the sprite, material, sorting layer, and color from the ProjectilePrefab's SpriteRenderer
		/// - the collider's size and offset from the ProjectilePrefab's CircleCollider2D
		/// - the tag and layer from the ProjectilePrefab's GameObject
		/// - any <see cref="DanmakuControlBehavior"/> on the ProjectilePrefab will be included as additional <see cref="IDanmakuController"/> that will affect the behavior of this bullet
		/// </summary>
		/// <param name="prefab">the ProjectilePrefab to match.</param>
		public void MatchPrefab(DanmakuPrefab prefab) {
			if (this.prefab != prefab) {
				this.prefab = prefab;
				this.runtime = prefab.GetRuntime();
				//Vector2 scale = transform.localScale = runtime.cachedScale;
				//renderer.sharedMaterial = runtime.cachedMaterial;
				//renderer.sortingLayerID = runtime.cachedSortingLayer;
				Vector2 scale = runtime.cachedScale;
				colliderOffset = scale.Hadamard2(runtime.cachedColliderOffset);
				colliderRadius = runtime.cachedColliderRadius * scale.Max();
				//tag = gameObject.tag = runtime.cachedTag;
				tag = runtime.cachedTag;
				symmetric = runtime.symmetric;
			}

			Color = runtime.cachedColor;
			Scale = 1f;
			layer = runtime.cachedLayer;
			colliderMask = collisionMask [layer];

			controllerUpdate += runtime.ExtraControllers;
		}

		#region IPooledObject implementation

		private IPool pool;
		public IPool Pool {
			get {
				return pool;
			}
			set {
				pool = value;
			}
		}

		internal bool is_active;

		public bool IsActive {
			get {
				return is_active;
			}
		}

		/// <summary>
		/// Activates this instance.
		/// Calling this on a already fired projectile does nothing.
		/// Calling this on a projectile marked for deactivation will unmark the projectile and keep it from deactivating.
		/// </summary>
		public void Activate () {
			to_deactivate = false;
			//gameObject.SetActive (true);
			//renderer.enabled = true;
			is_active = true;
			BoundsCheck = true;
			CollisionCheck = true;
			runtime.Add(this);
		}
		
		/// <summary>
		/// Marks the Danmaku for deactivation, and the Danmaku will deactivate and return to the ProjectileManager after 
		/// finishing processing current updates, or when the Danmaku is next updated
		/// If Danmaku needs to be deactivated in a moment when it is not being updated (i.e. when the game is paused), use <see cref="DeactivateImmediate"/> instead.
		/// </summary>
		public void Deactivate()  {
			to_deactivate = true;
		}

		#endregion

		/// <summary>
		/// Adds this projectile to the given ProjectileGroup
		/// </summary>
		/// <param name="group">the group this Danmaku is to be added to</param>
		public void AddToGroup(DanmakuGroup group) {
			groups.Add (group);
			group.group.Add (this);
			groupCountCache++;
			groupCheck = groupCountCache > 0;
		}

		/// <summary>
		/// Removes this projectile from the given ProjectileGroup
		/// </summary>
		/// <param name="group">the group this Danmaku is to be removed from</param>
		public void RemoveFromGroup(DanmakuGroup group) {
			groups.Remove (group);
			group.group.Remove (this);
			groupCountCache--;
			groupCheck = groupCountCache > 0;
		}

		/// <summary>
		/// Immediately deactivates this Danmaku and returns it to the pool it came from
		/// Calling this generally unadvised. Use <see cref="Deactivate"/> whenever possible.
		/// This method should only be used when dealing with Projectiles while the game is paused or when ProjectileManager is not enabled
		/// </summary>
		public void DeactivateImmediate() {
			for (int i = 0; i < groups.Count; i++) {
				groups[i].group.Remove (this);
			}
			groups.Clear ();
			if(tasks != null)
				tasks.Clear ();
			groupCountCache = 0;
			groupCheck = false;
			controllerUpdate = null;
			controllerCheck = false;
			Damage = 0;
			frames = 0;
			runtime.Remove(this);
			is_active = false;
			//gameObject.SetActive (false);
			//renderer.enabled = false;
			Pool.Return (this);
			//if (extraComponents != null) {
			//	while(extraComponents.Count > 0) {
			//		Object.Destroy(extraComponents.Pop());
			//	}
			//}
			//ProjectileManager.Return (this);
		}

		public override int GetHashCode () {
			return poolIndex;;
		}

//		public override bool Equals (object obj) {
//			return this == (obj as Danmaku);
//		}
//
//		public static bool operator ==(Danmaku d1, Danmaku d2) {
//			bool d1null = (object)d1 == null;
//			bool d2null = (object)d2 == null;
//			if(d1null && d2null)
//				return true;
//			if(d1null && !d2null)
//				return d2.is_active;
//			if(!d1null && d2null)
//				return d1.is_active;
//			return System.Object.ReferenceEquals(d1, d2);
//		}
//
//		public static bool operator !=(Danmaku d1, Danmaku d2) {
//			return !(d1 == d2);
//		}
	}
}
