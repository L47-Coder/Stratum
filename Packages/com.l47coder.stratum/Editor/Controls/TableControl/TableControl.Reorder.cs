using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratum.Editor
{
    public sealed partial class TableControl
    {
        private static void HookRowIndexPendingUpdate() { if (_rowIndexUpdateHooked) return; EditorApplication.update += RowIndexPendingUpdate; _rowIndexUpdateHooked = true; }
        private static void UnhookRowIndexPendingUpdate() { if (!_rowIndexUpdateHooked) return; EditorApplication.update -= RowIndexPendingUpdate; _rowIndexUpdateHooked = false; }

        private static void RowIndexPendingUpdate()
        {
            var owner = _rowIndexPendingOwner;
            if (owner == null) { UnhookRowIndexPendingUpdate(); return; }

            if (!owner._rowIndexInteractDown || _reorder != null)
            {
                if (_rowIndexPendingOwner == owner) _rowIndexPendingOwner = null;
                UnhookRowIndexPendingUpdate();
                return;
            }

            if (GUIUtility.hotControl != owner._rowIndexInteractControlId ||
                owner._rowIndexInteractSearchingAtPress || !owner.CanDrag)
            {
                owner._rowIndexInteractDown = false;
                owner.ReleaseRowIndexPendingOwner();
                return;
            }

            if (EditorApplication.timeSinceStartup - owner._rowIndexInteractPressTime < RowIndexHoldToDragSeconds) return;

            owner.BeginReorderSession(owner._rowIndexInteractControlId, owner._rowIndexInteractDataIndex,
                owner._rowIndexInteractRowRect, owner._rowIndexInteractRowHeight,
                owner._rowIndexInteractListCount, owner._rowIndexInteractPressPos.y);
        }

        private void CaptureRowIndexPendingForHold() { _rowIndexPendingOwner = this; HookRowIndexPendingUpdate(); }
        private void ReleaseRowIndexPendingOwner() { if (_rowIndexPendingOwner == this) _rowIndexPendingOwner = null; UnhookRowIndexPendingUpdate(); }
        private void ClearRowIndexInteractAfterReorderOrCancel() { _rowIndexInteractDown = false; ReleaseRowIndexPendingOwner(); }

        private void FlushRowIndexInteractClickSelect<T>(List<T> list)
        {
            var e = Event.current;
            if (e.type != EventType.MouseUp || e.button != 0) return;
            if (!_rowIndexInteractDown) return;
            if (GUIUtility.hotControl != _rowIndexInteractControlId) return;
            if (_reorder != null) return;

            var wasSearching = _rowIndexInteractSearchingAtPress;
            var idx = _rowIndexInteractDataIndex;
            GUIUtility.hotControl = 0;
            ClearRowIndexInteractAfterReorderOrCancel();

            if (!CanSelect || wasSearching) return;
            if (idx < 0 || idx >= list.Count) return;

            if (_selectedIndex == idx) _selectedIndex = -1;
            else { _selectedIndex = idx; _onRowSelected?.Invoke(idx); }
            RequestGuiVisualRefresh();
        }

        private void TryPromoteRowIndexToDrag(Rect rowRect, int dataIndex, int listCount, float rowHeight, int controlId)
        {
            if (_reorder != null) return;
            if (!_rowIndexInteractDown || GUIUtility.hotControl != controlId) return;
            if (dataIndex != _rowIndexInteractDataIndex) return;
            if (_rowIndexInteractSearchingAtPress || !CanDrag) return;

            var e = Event.current;
            if (e.type == EventType.MouseDrag &&
                (e.mousePosition - _rowIndexInteractPressPos).sqrMagnitude >= RowIndexDragSlopPixels * RowIndexDragSlopPixels)
            {
                BeginReorderSession(controlId, dataIndex, rowRect, rowHeight, listCount, e.mousePosition.y);
                e.Use();
            }
        }

        private void OnRowIndexInteractIgnore()
        {
            if (Event.current.rawType != EventType.Ignore) return;
            if (!_rowIndexInteractDown || _reorder != null) return;
            if (GUIUtility.hotControl != _rowIndexInteractControlId) return;
            GUIUtility.hotControl = 0;
            ClearRowIndexInteractAfterReorderOrCancel();
        }

        private void BeginReorderSession(int controlId, int rowIndex, Rect rowRect, float rowHeight, int arraySize, float pickMouseY)
        {
            ClearRowIndexInteractAfterReorderOrCancel();
            _draggingOwner = this;
            _reorder = new ReorderSession
            {
                ArraySize = arraySize,
                ControlId = controlId,
                SourceIndex = rowIndex,
                InsertSlot = rowIndex,
                PickupOffsetY = pickMouseY - rowRect.yMin,
                SourceRowHeight = rowHeight,
                RowCurrentY = new float[arraySize],
                RowTargetY = new float[arraySize],
                RowVelocityY = new float[arraySize],
                GapCurrentY = rowRect.yMin,
                GapTargetY = rowRect.yMin,
                DragRowYTarget = rowRect.yMin,
                DragRowY = rowRect.yMin,
                PositionsInitialized = false
            };
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            HookRepaintTick();
        }

        private void HandleActiveReorderLifecycle<T>(List<T> list)
        {
            if (_draggingOwner != this || _reorder == null) return;
            var e = Event.current;
            if (GUIUtility.hotControl != _reorder.ControlId)
            {
                if (e.rawType == EventType.MouseUp || e.rawType == EventType.Ignore) EndReorderSession();
                return;
            }
            if (e.type == EventType.MouseDrag) { e.Use(); return; }
            if (e.rawType == EventType.MouseUp && e.button == 0)
            {
                GUIUtility.hotControl = 0;
                ApplyReorder(list, _reorder.SourceIndex, _reorder.InsertSlot);
                EndReorderSession();
                e.Use();
                return;
            }
            if (e.rawType == EventType.Ignore) { GUIUtility.hotControl = 0; EndReorderSession(); }
        }

        private void ApplyReorder<T>(List<T> list, int from, int insertSlot)
        {
            var rowCount = list.Count;
            if (from < 0 || from >= rowCount) return;
            var dest = Mathf.Clamp(insertSlot, 0, rowCount - 1);
            if (dest == from) return;

            if (_selectedIndex == from) _selectedIndex = dest;
            else if (from < dest && _selectedIndex > from && _selectedIndex <= dest) _selectedIndex--;
            else if (from > dest && _selectedIndex >= dest && _selectedIndex < from) _selectedIndex++;

            var item = list[from];
            list.RemoveAt(from);
            list.Insert(dest, item);
            _onRowMoved?.Invoke(from, dest);
            GUI.changed = true;
        }

        private void EnsureSessionBuffers(int rowCount)
        {
            if (_reorder == null) return;
            if (_reorder.RowCurrentY != null && _reorder.RowCurrentY.Length == rowCount) return;
            Array.Resize(ref _reorder.RowCurrentY, rowCount);
            Array.Resize(ref _reorder.RowTargetY, rowCount);
            Array.Resize(ref _reorder.RowVelocityY, rowCount);
            _reorder.PositionsInitialized = false;
        }

        private void InitializeDragPositions(float topY, IReadOnlyList<float> rowHeights)
        {
            if (_reorder == null) return;
            var y = topY;
            for (var i = 0; i < rowHeights.Count; i++)
            {
                _reorder.RowCurrentY[i] = y; _reorder.RowTargetY[i] = y; _reorder.RowVelocityY[i] = 0f;
                y += rowHeights[i];
            }
            var sourceY = _reorder.SourceIndex >= 0 && _reorder.SourceIndex < _reorder.RowCurrentY.Length
                ? _reorder.RowCurrentY[_reorder.SourceIndex] : topY;
            _reorder.GapCurrentY = sourceY; _reorder.GapTargetY = sourceY; _reorder.GapVelocityY = 0f;
            _reorder.DragRowY = sourceY; _reorder.DragRowYTarget = sourceY;
            _reorder.LastBodyTopY = topY; _reorder.HasBodyTopY = true; _reorder.PositionsInitialized = true;
        }

        private void ApplyBodyScrollDelta(float bodyTopY)
        {
            if (_reorder == null) return;
            if (!_reorder.HasBodyTopY) { _reorder.LastBodyTopY = bodyTopY; _reorder.HasBodyTopY = true; return; }
            var deltaY = bodyTopY - _reorder.LastBodyTopY;
            if (Mathf.Abs(deltaY) < 0.01f) return;
            for (var i = 0; i < _reorder.RowCurrentY.Length; i++)
            {
                if (i == _reorder.SourceIndex) continue;
                _reorder.RowCurrentY[i] += deltaY;
                _reorder.RowTargetY[i] += deltaY;
            }
            _reorder.GapCurrentY += deltaY; _reorder.GapTargetY += deltaY;
            _reorder.LastBodyTopY = bodyTopY;
        }

        private void UpdateInsertSlotFromMouse(Rect bodyRect, IReadOnlyList<float> rowHeights, int rowCount)
        {
            if (_reorder == null || rowCount == 0) return;
            var dragRowTop = Mathf.Clamp(Event.current.mousePosition.y - _reorder.PickupOffsetY,
                bodyRect.yMin, Mathf.Max(bodyRect.yMin, bodyRect.yMax - _reorder.SourceRowHeight));
            var probeY = dragRowTop + _reorder.SourceRowHeight * 0.5f;
            var count = 0;
            var rowTop = bodyRect.yMin;
            for (var i = 0; i < rowHeights.Count; i++) { if (probeY > rowTop) count++; rowTop += rowHeights[i]; }
            _reorder.InsertSlot = Mathf.Clamp(count - 1, 0, Math.Max(0, rowCount - 1));
            _reorder.DragRowYTarget = dragRowTop;
        }

        private void UpdateTargets(float topY, IReadOnlyList<float> rowHeights)
        {
            if (_reorder == null) return;
            var slotCursor = 0;
            var y = topY;
            for (var i = 0; i < rowHeights.Count; i++)
            {
                if (i == _reorder.SourceIndex) continue;
                if (slotCursor == _reorder.InsertSlot) y += _reorder.SourceRowHeight;
                _reorder.RowTargetY[i] = y;
                y += rowHeights[i];
                slotCursor++;
            }
            var gapY = topY;
            var remaining = 0;
            for (var i = 0; i < rowHeights.Count; i++)
            {
                if (i == _reorder.SourceIndex) continue;
                if (remaining >= _reorder.InsertSlot) break;
                gapY += rowHeights[i];
                remaining++;
            }
            _reorder.GapTargetY = gapY;
        }

        private void StepSessionAnimation(float dt)
        {
            if (_reorder == null) return;
            for (var i = 0; i < _reorder.RowCurrentY.Length; i++)
            {
                if (i == _reorder.SourceIndex) continue;
                _reorder.RowCurrentY[i] = Mathf.SmoothDamp(_reorder.RowCurrentY[i], _reorder.RowTargetY[i],
                    ref _reorder.RowVelocityY[i], RowMoveSmoothTime, Mathf.Infinity, dt);
            }
            _reorder.GapCurrentY = Mathf.SmoothDamp(_reorder.GapCurrentY, _reorder.GapTargetY,
                ref _reorder.GapVelocityY, GapMoveSmoothTime, Mathf.Infinity, dt);
            _reorder.DragRowY = Mathf.SmoothDamp(_reorder.DragRowY, _reorder.DragRowYTarget,
                ref _reorder.DragRowVelY, DragRowSmoothTime, Mathf.Infinity, dt);
        }

        private static List<VisualRow> BuildVisualRows(
            Rect bodyRect, IReadOnlyList<float> rowHeights, IReadOnlyList<int> filteredIndices, bool dragging)
        {
            var rowCount = filteredIndices.Count;
            var rows = new List<VisualRow>(rowCount + (dragging ? 1 : 0));

            if (!dragging || _reorder == null)
            {
                var y = bodyRect.yMin;
                for (var i = 0; i < rowCount; i++)
                {
                    rows.Add(new VisualRow { RowIndex = filteredIndices[i], StripeIndex = i, DrawY = y, Height = rowHeights[i] });
                    y += rowHeights[i];
                }
                return rows;
            }

            rows.Add(new VisualRow
            {
                RowIndex = -1,
                StripeIndex = _reorder.InsertSlot,
                DrawY = _reorder.GapCurrentY,
                Height = _reorder.SourceRowHeight,
                IsGap = true
            });
            var stripe = 0;
            for (var i = 0; i < rowCount; i++)
            {
                if (i == _reorder.SourceIndex) continue;
                rows.Add(new VisualRow
                {
                    RowIndex = filteredIndices[i],
                    StripeIndex = stripe++,
                    DrawY = _reorder.RowCurrentY[i],
                    Height = rowHeights[i]
                });
            }
            return rows;
        }

        private void EndReorderSession() { _reorder = null; _draggingOwner = null; UnhookRepaintTick(); }

        private static void HookRepaintTick() { if (_dragTickHooked) return; EditorApplication.update += RepaintTick; _dragTickHooked = true; }
        private static void UnhookRepaintTick() { if (!_dragTickHooked) return; EditorApplication.update -= RepaintTick; _dragTickHooked = false; }

        private static void RepaintTick() { if (_reorder == null) { UnhookRepaintTick(); return; } (EditorWindow.mouseOverWindow ?? EditorWindow.focusedWindow)?.Repaint(); }

        private static float BeginFrameDelta()
        {
            var now = EditorApplication.timeSinceStartup;
            var dt = Mathf.Clamp((float)(now - _lastUpdateTime), 0.0025f, 0.05f);
            _lastUpdateTime = now;
            return dt;
        }
    }
}
