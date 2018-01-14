﻿using System.Collections;
using System.Linq;
using PlayGroup;
using Tilemaps.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Toggles the active state of the object by gathering all components and setting
///     their active state. It ignores network components so item can be synced
/// </summary>
public class VisibleBehaviour : NetworkBehaviour
{
	//Ignore these types
	private const string networkId = "NetworkIdentity";

	private const string networkT = "NetworkTransform";
	private const string customNetTransform = "CustomNetTransform";
	private const string objectBehaviour = "ObjectBehaviour";
	private const string regTile = "RegisterTile";
	private const string inputController = "InputController";
	private const string playerSync = "PlayerSync";
	private const string closetHandler = "ClosetPlayerHandler";

	public bool isPlayer;

	private readonly string[] neverDisabled =
	{
		networkId,
		networkT,
		customNetTransform,
		objectBehaviour,
		regTile,
		inputController,
		playerSync,
		closetHandler
	};

	public GameObject[] ignoreSpriteUpdates;
	private SpriteRenderer[] ignoredSpriteRenderers;

	public RegisterTile registerTile;

	/// <summary>
	///     This will also set the enabled state of every component
	/// </summary>
	[SyncVar(hook = nameof(UpdateState))] public bool visibleState = true;

	protected virtual void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
		PlayerScript pS = GetComponent<PlayerScript>();
		if (pS != null)
		{
			isPlayer = true;
		}

		ignoredSpriteRenderers = new SpriteRenderer[ignoreSpriteUpdates.Length];
		for (int i = 0; i < ignoreSpriteUpdates.Length; i++)
		{
			SpriteRenderer sr = ignoreSpriteUpdates[i].GetComponent<SpriteRenderer>();
			if (sr != null)
			{
				ignoredSpriteRenderers[i] = sr;
			}
		}
	}

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(3f);
		UpdateState(visibleState);
	}

	//For ObjectBehaviour to handle specific states with the various objects like players
	public virtual void OnVisibilityChange(bool state)
	{
	}

	private void UpdateState(bool _aliveState)
	{
		visibleState = _aliveState;
		OnVisibilityChange(_aliveState);

		MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>(true);
		Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < scripts.Length; i++)
		{
			if (CanBeDisabled(scripts[i]))
			{
				scripts[i].enabled = _aliveState;
			}
		}

		for (int i = 0; i < colliders.Length; i++)
		{
			colliders[i].enabled = _aliveState;
		}

		for (int i = 0; i < renderers.Length; i++)
		{
			// Cast and check cast
			SpriteRenderer sr = renderers[i] as SpriteRenderer;
			if (sr != null)
			{
				if (CanBeDisabled(sr))
				{
					sr.enabled = _aliveState;
				}
			}
			else
			{
				renderers[i].enabled = _aliveState;
			}
		}

		if (registerTile != null)
		{
			if (_aliveState)
			{
				registerTile.UpdatePosition();
			}
			else
			{
				registerTile.Unregister();
			}
		}
	}

	private bool CanBeDisabled(SpriteRenderer script)
	{
		return !ignoredSpriteRenderers.Contains(script);
	}

	private bool CanBeDisabled(MonoBehaviour script)
	{
		return !neverDisabled.Contains(script.GetType().Name);
	}
}