﻿using System;
using System.Drawing;
using System.ComponentModel;
#if __UNIFIED__
using GLKit;
using OpenGLES;
using Foundation;
using CoreAnimation;
#else
using MonoTouch.GLKit;
using MonoTouch.OpenGLES;
using MonoTouch.Foundation;
using MonoTouch.CoreAnimation;
#endif
#if __UNIFIED__
using RectangleF = CoreGraphics.CGRect;
using SizeF = CoreGraphics.CGSize;
using PointF = CoreGraphics.CGPoint;

#else
using nfloat=System.Single;
using nint=System.Int32;
using nuint=System.UInt32;
#endif

namespace Xamarin.Forms.Platform.iOS
{
	internal class OpenGLViewRenderer : ViewRenderer<OpenGLView, GLKView>
	{
		CADisplayLink _displayLink;

		public void Display(object sender, EventArgs eventArgs)
		{
			if (Element.HasRenderLoop)
				return;
			SetupRenderLoop(true);
		}

		protected override void Dispose(bool disposing)
		{
			if (_displayLink != null)
			{
				_displayLink.Invalidate();
				_displayLink.Dispose();
				_displayLink = null;

				if (Element != null)
					((IOpenGlViewController)Element).DisplayRequested -= Display;
			}

			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<OpenGLView> e)
		{
			if (e.OldElement != null)
				((IOpenGlViewController)e.OldElement).DisplayRequested -= Display;

			if (e.NewElement != null)
			{
				var context = new EAGLContext(EAGLRenderingAPI.OpenGLES2);
				var glkView = new GLKView(RectangleF.Empty) { Context = context, DrawableDepthFormat = GLKViewDrawableDepthFormat.Format24, Delegate = new Delegate(e.NewElement) };
				SetNativeControl(glkView);

				((IOpenGlViewController)e.NewElement).DisplayRequested += Display;

				SetupRenderLoop(false);
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == OpenGLView.HasRenderLoopProperty.PropertyName)
				SetupRenderLoop(false);
		}

		void SetupRenderLoop(bool oneShot)
		{
			if (_displayLink != null)
				return;
			if (!oneShot && !Element.HasRenderLoop)
				return;

			_displayLink = CADisplayLink.Create(() =>
			{
				var control = Control;
				var model = Element;
				if (control != null)
					control.Display();
				if (control == null || model == null || !model.HasRenderLoop)
				{
					_displayLink.Invalidate();
					_displayLink.Dispose();
					_displayLink = null;
				}
			});
			_displayLink.AddToRunLoop(NSRunLoop.Current, NSRunLoop.NSDefaultRunLoopMode);
		}

		class Delegate : GLKViewDelegate
		{
			readonly OpenGLView _model;

			public Delegate(OpenGLView model)
			{
				_model = model;
			}

			public override void DrawInRect(GLKView view, RectangleF rect)
			{
				var onDisplay = _model.OnDisplay;
				if (onDisplay == null)
					return;
				onDisplay(rect.ToRectangle());
			}
		}
	}
}