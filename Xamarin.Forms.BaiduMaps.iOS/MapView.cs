﻿using System;
using System.Diagnostics;

using CoreLocation;
using Xamarin.Forms.Platform.iOS;

using BMapMain;
using UIKit;
using CoreGraphics;

using System.Reflection;

namespace Xamarin.Forms.BaiduMaps.iOS
{
    public partial class MapRenderer : ViewRenderer<Map, BMKMapView>
    {
        private class MapViewDelegate : BMKMapViewDelegate
        {
            private MapRenderer map { get; }
            public MapViewDelegate(MapRenderer map)
            {
                this.map = map;
            }

            public override BMKAnnotationView MapViewViewForAnnotation(BMKMapView mapView, BMKAnnotation annotation)
            {
                if (typeof(BMKPointAnnotation) == annotation.GetType())
                {
                    Pin ann = map.Map.Pins.Find(annotation);
                    if (null != ann) {
                        BMKPinAnnotationView annotationView = new BMKPinAnnotationView(annotation, "myAnnotation");
                        annotationView.PinColor = BMKPinAnnotationColor.Purple;
                        annotationView.AnimatesDrop = ann.Animate;
                        annotationView.Draggable = ann.Draggable;
                        annotationView.Enabled3D = ann.Enabled3D;

                        if (null != ann.Image) {
                            annotationView.Image = ann.Image.ToNative();
                        }

                        /*var label = new Label();
                        label.Text = ann.Title;
                        label.Layout(new Rectangle(0, 0, 100, 50));
                        label.FontSize = 13;
                        label.BackgroundColor = Color.White;
                        label.Opacity = 0.8;
                        label.VerticalTextAlignment = label.HorizontalTextAlignment = TextAlignment.Center;
                        annotationView.PaopaoView = new BMKActionPaopaoView(
                            label.ToNative(new Rectangle(0, 0, 100, 50))
                        );*/

                        return annotationView;
                    }
                }

                Debug.WriteLine("MapViewViewForAnnotation: " + annotation.GetType());
                return null;
            }

            public override BMKOverlayView MapViewViewForOverlay(BMKMapView mapView, BMKOverlay overlay)
            {
                if (typeof(BMKPolyline) == overlay.GetType())
                {
                    Polyline poly = map.Map.Polylines.Find(overlay);
                    if (null != poly) {
                        BMKPolylineView lineView = new BMKPolylineView(overlay);
                        lineView.StrokeColor = poly.Color.ToUIColor();
                        lineView.LineWidth = poly.Width;

                        return lineView;
                    }
                }

                Debug.WriteLine("MapViewViewForOverlay: " + overlay.GetType());
                return null;
            }

            public override void MapViewOnClickedMapBlank(BMKMapView mapView, CLLocationCoordinate2D coordinate)
            {
                //Debug.WriteLine("点击: " + coordinate.ToCoordinate());
                map.Map.SendBlankClicked(coordinate.ToUnity());
            }

            public override void MapViewOnClickedMapPoi(BMKMapView mapView, BMKMapPoi mapPoi)
            {
                //Debug.WriteLine("点击POI: " + coordinate.ToCoordinate());
                Poi poi = new Poi {
                    Coordinate = mapPoi.Pt.ToUnity(),
                    Description = mapPoi.Description
                };

                map.Map.SendPoiClicked(poi);
            }

            public override void MapViewOnClickedBMKOverlayView(BMKMapView mapView, BMKOverlayView overlayView)
            {
                //Debug.WriteLine("点击Overlay: " + coordinate.ToCoordinate());
            }

            public override void MapViewOnDoubleClick(BMKMapView mapView, CLLocationCoordinate2D coordinate)
            {
                //Debug.WriteLine("双击: " + coordinate.ToCoordinate());
                map.Map.SendDoubleClicked(coordinate.ToUnity());
            }

            //private Coordinate lastLongClick;
            //private CLLocationCoordinate2D lastLongClickCoord = new CLLocationCoordinate2D(0, 0);
            public override void MapViewOnLongClick(BMKMapView mapView, CLLocationCoordinate2D coordinate)
            {
                // IOS SDK 3.0 中存在抬起手指又触发一次，和长按拖动也会不停触发的bug
                if (map.isLongPressReady) {
                    map.Map.SendLongClicked(coordinate.ToUnity());
                    map.isLongPressReady = false;
                }
            }

            public override void MapViewDidSelectAnnotationView(BMKMapView mapView, BMKAnnotationView view)
            {
                Pin annotation = map.Map.Pins.Find(view.Annotation);
                annotation?.SendClicked();
            }

            public override void MapViewDidDeselectAnnotationView(BMKMapView mapView, BMKAnnotationView view)
            {
                //Pin annotation = Map.Pins.Find(view.Annotation);
                //annotation?.SendDeselected();
            }

            public override void MapViewAnnotationViewDidChangeDragStateFromOldState(
                BMKMapView mapView, BMKAnnotationView view,
                BMKAnnotationViewDragState newState, BMKAnnotationViewDragState oldState)
            {
                //Debug.WriteLine(oldState + " => " + newState);
                // None => Starting
                // Starting => Dragging
                // Dragging => Dragging
                // Dragging => Ending
                Pin annotation = map.Map.Pins.Find(view.Annotation);
                if (BMKAnnotationViewDragState.Starting == newState) {
                    annotation?.SendDrag(AnnotationDragState.Starting);
                    return;
                }

                if (BMKAnnotationViewDragState.Dragging == newState
                    && null != annotation)
                {
                    map.pointAnnotationImpl.NotifyUpdate(annotation);
                    annotation.SendDrag(AnnotationDragState.Dragging);
                    return;
                }

                if (BMKAnnotationViewDragState.Ending == newState
                    && null != annotation)
                {
                    map.pointAnnotationImpl.NotifyUpdate(annotation);
                    annotation.SendDrag(AnnotationDragState.Ending);
                }
            }

            public override void MapViewDidFinishLoading(BMKMapView mapView)
            {
                mapView.ViewWillAppear();
                map.Map.SendLoaded();
            }

            public override void MapStatusDidChanged(BMKMapView mapView)
            {
                bool changed = false;

                Coordinate center = mapView.CenterCoordinate.ToUnity();
                if (map.Map.Center != center) {
                    map.Map.SetValueSilent(Map.CenterProperty, center);
                    changed = true;
                }

                float zoom = mapView.ZoomLevel;
                if (Math.Abs(map.Map.ZoomLevel - zoom) > 0.01) {
                    map.Map.SetValueSilent(Map.ZoomLevelProperty, zoom);
                    changed = true;
                }

                if (changed) {
                   map.Map.SendStatusChanged();
                }
            }
        }
    }
}