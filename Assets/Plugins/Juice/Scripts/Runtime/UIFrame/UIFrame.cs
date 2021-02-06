﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Juice
{
	public class UIFrame : MonoBehaviour
	{
		public event WindowLayer.WindowChangeHandler CurrentWindowChanged;

		public Canvas MainCanvas
		{
			get
			{
				if (mainCanvas == null)
				{
					mainCanvas = GetComponentInChildren<Canvas>();
				}

				return mainCanvas;
			}
		}

		public Camera UICamera => MainCanvas.worldCamera;
		public IWindow CurrentWindow => windowLayer.CurrentWindow;

		[SerializeField] private bool initializeOnAwake = true;

		private Canvas mainCanvas;
		private PanelLayer panelLayer;
		private WindowLayer windowLayer;
		private GraphicRaycaster graphicRaycaster;

		private readonly Dictionary<Type, IView> registeredViews = new Dictionary<Type, IView>();
		private readonly HashSet<object> blockInteractionRequesters = new HashSet<object>();

		private void Reset()
		{
			initializeOnAwake = true;
		}

		private void Awake()
		{
			if (initializeOnAwake)
			{
				Initialize();
			}
		}

		public virtual void Initialize()
		{
			if (panelLayer == null)
			{
				panelLayer = GetComponentInChildren<PanelLayer>();

				if (panelLayer == null)
				{
					Debug.LogError("UI Frame lacks Panel Layer!");
				}
				else
				{
					panelLayer.Initialize(this);
				}
			}

			if (windowLayer == null)
			{
				windowLayer = GetComponentInChildren<WindowLayer>();

				if (windowLayer == null)
				{
					Debug.LogError("UI Frame lacks Window Layer!");
				}
				else
				{
					windowLayer.Initialize(this);
					windowLayer.CurrentWindowChanged += CurrentWindowChanged;
				}
			}

			graphicRaycaster = MainCanvas.GetComponent<GraphicRaycaster>();
		}

		public void RegisterView<T>(T view) where T : IView
		{
			if (IsViewValid(view))
			{
				Type viewType = view.GetType();

				if (typeof(IPanel).IsAssignableFrom(viewType))
				{
					IPanel viewAsPanel = view as IPanel;
					ProcessViewRegister(viewAsPanel, panelLayer);
				}
				else if (typeof(IWindow).IsAssignableFrom(viewType))
				{
					IWindow viewAsWindow = view as IWindow;
					ProcessViewRegister(viewAsWindow, windowLayer);
				}
				else
				{
					Debug.LogError($"The View type {typeof(T).Name} must implement {nameof(IPanel)} or {nameof(IWindow)}.");
				}
			}
		}

		public void UnregisterView<T>(T view) where T : IView
		{
			Type viewType = view.GetType();

			if (registeredViews.ContainsKey(viewType))
			{
				if (typeof(IPanel).IsAssignableFrom(viewType))
				{
					IPanel viewAsPanel = view as IPanel;
					ProcessViewUnregister(viewAsPanel, panelLayer);
				}
				else if (typeof(IWindow).IsAssignableFrom(viewType))
				{
					IWindow viewAsWindow = view as IWindow;
					ProcessViewUnregister(viewAsWindow, windowLayer);
				}
			}
			else
			{
				Debug.LogError($"Provided view {viewType.Name} was not registered.");
			}
		}

		public IPanelShowLauncher ShowPanel<T>() where T : IPanel
		{
			return new PanelShowLauncher(typeof(T), ShowPanel);
		}

		public IWindowShowLauncher ShowWindow<T>() where T : IWindow
		{
			return new WindowShowLauncher(typeof(T), ShowWindow);
		}

		public IPanelHideLauncher HidePanel<T>() where T : IPanel
		{
			return new PanelHideLauncher(typeof(T), HidePanel);
		}

		public IWindowHideLauncher HideWindow<T>() where T : IWindow
		{
			return new WindowHideLauncher(typeof(T), HideWindow);
		}

		public IWindowHideLauncher CloseCurrentWindow()
		{
			return new WindowHideLauncher(CurrentWindow.GetType(), HideWindow);
		}

		public bool IsViewRegistered<T>() where T : IView
		{
			return registeredViews.ContainsKey(typeof(T));
		}

		public void BlockInteraction(object requester)
		{
			blockInteractionRequesters.Add(requester);

			if (blockInteractionRequesters.Count == 1)
			{
				BlockInteraction();
			}
		}

		public void UnblockInteraction(object requester)
		{
			blockInteractionRequesters.Remove(requester);

			if (blockInteractionRequesters.Count <= 0)
			{
				UnblockInteraction();
			}
		}

		private bool IsViewValid(IView view)
		{
			Component viewAsComponent = view as Component;

			if (viewAsComponent == null)
			{
				Debug.LogError($"The View to register must derive from {nameof(Component)}");
				return false;
			}

			if (registeredViews.ContainsKey(view.GetType()))
			{
				Debug.LogError($"{view.GetType().Name} already registered.");
				return false;
			}

			return true;
		}

		private void ProcessViewRegister<TView, TShowSettings, THideSettings>(TView view, Layer<TView, TShowSettings, THideSettings> layer)
			where TView : IView
			where TShowSettings : IViewShowSettings
			where THideSettings : IViewHideSettings
		{
			Component viewAsComponent = view as Component;

			if (viewAsComponent != null)
			{
				viewAsComponent.gameObject.SetActive(false);
				layer.ReparentView(view, viewAsComponent.transform);
			}

			Type viewType = view.GetType();
			registeredViews.Add(viewType, view);
			layer.RegisterView(view);

			view.Showing += OnViewShowing;
			view.Shown += OnViewShown;
			view.Hiding += OnViewHiding;
			view.Hidden += OnViewHidden;
		}

		private void ProcessViewUnregister<TView, TShowSettings, THideSettings>(TView view, Layer<TView, TShowSettings, THideSettings> layer)
			where TView : IView
			where TShowSettings : IViewShowSettings
			where THideSettings : IViewHideSettings
		{
			Component viewAsComponent = view as Component;

			if (viewAsComponent != null)
			{
				viewAsComponent.gameObject.SetActive(false);
				viewAsComponent.transform.SetParent(null);
			}

			Type viewType = view.GetType();
			registeredViews.Remove(viewType);
			layer.UnregisterView(view);

			view.Showing -= OnViewShowing;
			view.Shown -= OnViewShown;
			view.Hiding -= OnViewHiding;
			view.Hidden -= OnViewHidden;
		}

		private void OnViewShowing(ITransitionable view)
		{
			BlockInteraction(view);
		}

		private void OnViewShown(ITransitionable view)
		{
			UnblockInteraction(view);
		}

		private void OnViewHiding(ITransitionable view)
		{
			BlockInteraction(view);
		}

		private void OnViewHidden(ITransitionable view)
		{
			UnblockInteraction(view);
		}

		private async Task ShowPanel(PanelShowSettings settings)
		{
			await panelLayer.ShowView(settings);
		}

		private async Task ShowWindow(WindowShowSettings settings)
		{
			await windowLayer.ShowView(settings);
		}

		private async Task HidePanel(PanelHideSettings settings)
		{
			await panelLayer.HideView(settings);
		}

		private async Task HideWindow(WindowHideSettings settings)
		{
			await windowLayer.HideView(settings);
		}

		private void BlockInteraction()
		{
			if (graphicRaycaster)
			{
				graphicRaycaster.enabled = false;
			}

			foreach (var current in registeredViews)
			{
				current.Value.AllowsInteraction = false;
			}
		}

		private void UnblockInteraction()
		{
			if (graphicRaycaster)
			{
				graphicRaycaster.enabled = true;
			}

			foreach (var current in registeredViews)
			{
				current.Value.AllowsInteraction = true;
			}
		}
	}
}
