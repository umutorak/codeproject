using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using VixCOM;

namespace Vestris.VMWareLib
{
    /// <summary>
    /// A VMWare snapshot.
    /// </summary>
    public class VMWareSnapshot : VMWareVixHandle<ISnapshot>
    {
        private IVM _vm = null;
        private VMWareSnapshotCollection _childSnapshots = null;

        public VMWareSnapshot(IVM vm, ISnapshot snapshot)
            : base(snapshot)
        {
            _vm = vm;
        }

        /// <summary>
        /// Restores the virtual machine to the state when the specified snapshot was created.
        /// </summary>
        public void RevertToSnapshot(int powerOnOptions, int timeoutInSeconds)
        {
            VMWareJob job = new VMWareJob(_vm.RevertToSnapshot(_handle, powerOnOptions, null, null));
            job.Wait(timeoutInSeconds);
        }

        /// <summary>
        /// Restores the virtual machine to the state when the specified snapshot was created.
        /// </summary>
        public void RevertToSnapshot(int timeoutInSeconds)
        {
            RevertToSnapshot(Constants.VIX_VMPOWEROP_NORMAL, timeoutInSeconds);
        }

        /// <summary>
        /// Restores the virtual machine to the state when the specified snapshot was created.
        /// </summary>
        public void RevertToSnapshot()
        {
            RevertToSnapshot(VMWareInterop.Timeouts.RevertToSnapshotTimeout);
        }

        /// <summary>
        /// Remove/delete this snapshot.
        /// </summary>
        public void RemoveSnapshot()
        {
            RemoveSnapshot(VMWareInterop.Timeouts.RemoveSnapshotTimeout);
        }

        /// <summary>
        /// Remove/delete this snapshot.
        /// </summary>
        /// <param name="timeoutInSeconds">timeout in seconds</param>
        public void RemoveSnapshot(int timeoutInSeconds)
        {
            VMWareJob job = new VMWareJob(_vm.RemoveSnapshot(_handle, 0, null));
            job.Wait(timeoutInSeconds);
        }

        /// <summary>
        /// Child snapshots.
        /// </summary>
        public VMWareSnapshotCollection ChildSnapshots
        {
            get
            {
                if (_childSnapshots == null)
                {
                    VMWareSnapshotCollection childSnapshots = new VMWareSnapshotCollection(_vm);
                    int nChildSnapshots = 0;
                    VMWareInterop.Check(_handle.GetNumChildren(out nChildSnapshots));
                    for (int i = 0; i < nChildSnapshots; i++)
                    {
                        ISnapshot childSnapshot = null;
                        VMWareInterop.Check(_handle.GetChild(i, out childSnapshot));
                        childSnapshots.Add(new VMWareSnapshot(_vm, childSnapshot));
                    }
                    _childSnapshots = childSnapshots;
                }
                return _childSnapshots;
            }
        }

        /// <summary>
        /// Display name of the snapshot.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return GetProperty<string>(Constants.VIX_PROPERTY_SNAPSHOT_DISPLAYNAME);
            }
        }

        /// <summary>
        /// Display name of the snapshot.
        /// </summary>
        public string Description
        {
            get
            {
                return GetProperty<string>(Constants.VIX_PROPERTY_SNAPSHOT_DESCRIPTION);
            }
        }

        /// <summary>
        /// Complete snapshot path, from root.
        /// </summary>
        public string Path
        {
            get
            {
                ISnapshot parentSnapshot = null;
                VMWareInterop.Check(_handle.GetParent(out parentSnapshot));
                // hack: get the parent's parent snapshot: if this fails, we're looking at the root
                ISnapshot parentsParentSnapshot = null;
                return (parentSnapshot.GetParent(out parentsParentSnapshot) != VixCOM.Constants.VIX_OK)
                    ? DisplayName
                    : System.IO.Path.Combine(new VMWareSnapshot(_vm, parentSnapshot).Path, DisplayName);
            }
        }

        /// <summary>
        /// The power state of this snapshot, an OR-ed set of VIX_POWERSTATE_* values.
        /// </summary>
        public int PowerState
        {
            get
            {
                return GetProperty<int>(Constants.VIX_PROPERTY_SNAPSHOT_POWERSTATE);
            }
        }

        /// <summary>
        /// Returns true if the snapshot is replayable.
        /// </summary>
        public bool IsReplayable
        {
            get
            {
                return GetProperty<bool>(Constants.VIX_PROPERTY_SNAPSHOT_IS_REPLAYABLE);
            }
        }
    }
}
