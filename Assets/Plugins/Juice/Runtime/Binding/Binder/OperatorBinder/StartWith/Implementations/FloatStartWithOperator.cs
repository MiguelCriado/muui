﻿using UnityEngine;

namespace Juice
{
	public class FloatStartWithOperator : StartWithOperator<float>
	{
		protected override ConstantBindingInfo<float> InitialValue => initialValue;

		[SerializeField] private FloatConstantBindingInfo initialValue = new FloatConstantBindingInfo();
	}
}