﻿using System;
using UnityEngine;

namespace Juice
{
	[RequireComponent(typeof(RectTransform))]
	[RequireComponent(typeof(ViewModelComponent))]
	[RequireComponent(typeof(InteractionBlockingTracker))]
	public abstract class View<T> : Widget, IView, IViewModelInjector
		where T : IViewModel
	{
		public delegate void ViewModelEventHandler(View<T> source, T lastViewModel, T newViewModel);

		public event ViewEventHandler CloseRequested;
		public event ViewEventHandler ViewDestroyed;
		public event ViewModelEventHandler ViewModelChanged;

		public bool IsInteractable
		{
			get => blockingTracker.IsInteractable;
			set => blockingTracker.IsInteractable = value;
		}

		public Type InjectionType => typeof(T);
		public ViewModelComponent Target => targetComponent;

		[SerializeField, HideInInspector] private ViewModelComponent targetComponent;
		[SerializeField, HideInInspector] private InteractionBlockingTracker blockingTracker;

		protected virtual void Reset()
		{
			RetrieveRequiredComponents();
		}

		protected override void Awake()
		{
			RetrieveRequiredComponents();
			Target.ViewModelChanged += OnTargetComponentViewModelChanged;
		}

		public void SetViewModel(IViewModel viewModel)
		{
			if (viewModel != null)
			{
				if (viewModel is T typedViewModel)
				{
					SetViewModel(typedViewModel);
				}
				else
				{
					Debug.LogError($"ViewModel passed have wrong type! ({viewModel.GetType()} instead of {typeof(T)})", this);
				}
			}
		}

		protected virtual void SetViewModel(T viewModel)
		{
			if (targetComponent)
			{
				targetComponent.ViewModel = viewModel;
			}
		}

		protected virtual void OnViewModelChanged(IViewModel lastViewModel, IViewModel newViewModel)
		{
			ViewModelChanged?.Invoke(this, (T)lastViewModel, (T)newViewModel);
		}

		private void RetrieveRequiredComponents()
		{
			if (!targetComponent)
			{
				targetComponent = GetComponent<ViewModelComponent>();
			}

			if (!blockingTracker)
			{
				blockingTracker = GetComponent<InteractionBlockingTracker>();
			}
		}

		private void OnTargetComponentViewModelChanged(ViewModelComponent source, IViewModel lastViewModel, IViewModel newViewModel)
		{
			OnViewModelChanged(lastViewModel, newViewModel);
		}
	}
}
