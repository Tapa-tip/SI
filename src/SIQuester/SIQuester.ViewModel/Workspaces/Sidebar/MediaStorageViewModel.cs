﻿using SIPackages;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Defines a media storage view model.
    /// </summary>
    public sealed class MediaStorageViewModel : WorkspaceViewModel
    {
        private readonly QDocument _document = null;

        /// <summary>
        /// Добавленные файлы
        /// </summary>
        private readonly List<MediaItemViewModel> _added = new();

        /// <summary>
        /// Удалённые файлы
        /// </summary>
        private readonly List<MediaItemViewModel> _removed = new();

        /// <summary>
        /// Переименованные файлы
        /// </summary>
        private readonly List<Tuple<string, string>> _renamed = new();

        /// <summary>
        /// Пути для файлов, ещё не загруженных в коллекцию (не закоммиченных)
        /// </summary>
        private readonly Dictionary<MediaItemViewModel, Tuple<string, FileStream>> _streams = new();

        private bool _blockFlag = false;

        private bool _hasPendingChanges = false;

        public bool HasPendingChanges
        {
            get => _hasPendingChanges;
            set
            {
                if (_hasPendingChanges != value)
                {
                    _hasPendingChanges = value;
                    OnPropertyChanged();

                    if (_hasPendingChanges)
                    {
                        HasChanged?.Invoke();
                    }
                }
            }
        }

        internal event Action<IChange> Changed;

        internal void OnChanged(IChange change) => Changed?.Invoke(change);

        /// <summary>
        /// Collection files.
        /// </summary>
        public ObservableCollection<MediaItemViewModel> Files { get; } = new();

        public ICommand AddItem { get; private set; }

        public ICommand DeleteItem { get; private set; }

        private readonly string _header;

        private readonly string _name;

        public override string Header => _header;

        public event Action HasChanged;

        public MediaStorageViewModel(QDocument document, DataCollection collection, string header)
        {
            _document = document;
            _header = header;
            _name = collection.Name;

            FillFiles(collection);

            AddItem = new SimpleCommand(AddItem_Executed);
            DeleteItem = new SimpleCommand(Delete_Executed);
        }

        private void FillFiles(DataCollection collection)
        {
            foreach (var item in collection)
            {
                var named = CreateItem(item);
                Files.Add(named);
            }
        }

        private MediaItemViewModel CreateItem(string item)
        {
            var named = new MediaItemViewModel(new Named(item), _name, () => Wrap(item));

            named.Model.PropertyChanged += Named_PropertyChanged;

            return named;
        }

        private void Named_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_blockFlag)
            {
                return;
            }

            var ext = (ExtendedPropertyChangedEventArgs<string>)e;
            var item = (Named)sender;

            if (Files.Count(obj => obj.Model.Name == item.Name) > 1)
            {
                SafeRename(item, ext.OldValue);
                return;
            }

            var newValue = item.Name;
            var renamedExisting = !_added.Any(mi => mi.Model == item);
            Tuple<string, string> tuple = null;

            if (renamedExisting)
            {
                tuple = Tuple.Create(ext.OldValue, item.Name.Replace("%", ""));
                _renamed.Add(tuple);
            }

            OnChanged(new CustomChange(
                () =>
                {
                    if (renamedExisting)
                    {
                        var found = false;

                        for (int i = _renamed.Count - 1; i >= 0; i--)
                        {
                            if (_renamed[i] == tuple)
                            {
                                _renamed.RemoveAt(i);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            _renamed.Add(Tuple.Create(item.Name, ext.OldValue));
                            HasPendingChanges = IsChanged();
                        }
                    }

                    SafeRename(item, ext.OldValue);

                },
                () =>
                {
                    SafeRename(item, newValue);

                    if (renamedExisting)
                    {
                        var last = _renamed.LastOrDefault();

                        if (last != null && last.Item1 == tuple.Item2 && last.Item2 == tuple.Item1)
                        {
                            _renamed.RemoveAt(_renamed.Count - 1);
                        }
                        else
                        {
                            _renamed.Add(tuple);
                        }

                        HasPendingChanges = IsChanged();
                    }
                }));
            
            HasPendingChanges = IsChanged();
        }

        private void SafeRename(Named item, string name)
        {
            _blockFlag = true;

            try
            {
                item.Name = name;
            }
            finally
            {
                _blockFlag = false;
            }
        }

        private bool IsChanged() => _added.Count > 0 || _removed.Count > 0 || _renamed.Count > 0;

        private void Delete_Executed(object arg)
        {
            try
            {
                var item = (MediaItemViewModel)arg;

                var uri = Wrap(item.Model.Name);
                item.MediaSource = null;

                PreviewRemove(item);

                OnChanged(new CustomChange(
                    () =>
                    {
                        PreviewAdd(item, uri.Uri);
                        HasPendingChanges = IsChanged();
                    },
                    () =>
                    {
                        PreviewRemove(item);
                        HasPendingChanges = IsChanged();
                    }));

                HasPendingChanges = IsChanged();
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private void PreviewRemove(MediaItemViewModel name)
        {
            if (_added.Contains(name))
            {
                _added.Remove(name);
                _streams[name].Item2.Dispose();
                _streams.Remove(name);
            }
            else
            {
                _removed.Add(name);
            }

            Files.Remove(name);
            OnPropertyChanged(nameof(Files));
        }

        public async Task CommitAsync(DataCollection collection, CancellationToken cancellationToken = default)
        {
            foreach (var item in _removed.ToArray())
            {
                collection.RemoveFile(item.Model.Name);
                item.PropertyChanged -= Named_PropertyChanged;
                _removed.Remove(item);
            }

            foreach (var item in _added.ToArray())
            {
                try
                {
                    using var fs = _streams[item].Item2;
                    await collection.AddFileAsync(item.Model.Name, fs, cancellationToken);
                }
                catch (Exception exc)
                {
                    OnError(exc);
                }
                
                _added.Remove(item);
                _streams.Remove(item);
            }

            foreach (var item in _renamed.ToArray())
            {
                await collection.RenameFileAsync(item.Item1, item.Item2, cancellationToken);
                _renamed.Remove(item);
            }

            HasPendingChanges = false;
        }

        public async Task ApplyToAsync(DataCollection collection, bool final = false)
        {
            foreach (var item in _removed.ToArray())
            {
                collection.RemoveFile(item.Model.Name);
            }

            foreach (var item in _added.ToArray())
            {
                var fs = _streams[item].Item2;

                if (final)
                {
                    using (fs)
                    {
                        await collection.AddFileAsync(item.Model.Name, fs);
                    }
                }
                else
                {
                    await collection.AddFileAsync(item.Model.Name, fs);
                    fs.Position = 0;
                }
            }

            foreach (var item in _renamed.ToArray())
            {
                await collection.RenameFileAsync(item.Item1, item.Item2);
            }

            if (final)
            {
                _added.Clear();
                _removed.Clear();
                _renamed.Clear();
            }
        }

        private void AddItem_Executed(object arg)
        {
            var files = PlatformSpecific.PlatformManager.Instance.ShowMediaOpenUI();

            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                AddFile(file);
            }

            HasPendingChanges = IsChanged();
        }

        internal void AddFile(string file, string name = null)
        {
            var localName = name ?? Path.GetFileName(file).Replace("%", "");

            if (Files.Any(named => named.Model.Name == localName))
            {
                var ind = 1;
                var ext = Path.GetExtension(localName);
                var baseName = Path.GetFileNameWithoutExtension(localName);
                string newName = null;

                do
                {
                    newName = string.Format("{0}_{1}{2}", baseName, ind++, ext);
                } while (Files.Any(named => named.Model.Name == newName));

                localName = newName;
            }

            var item = CreateItem(localName);
            PreviewAdd(item, file);

            OnChanged(new CustomChange(
                () =>
                {
                    PreviewRemove(item);
                    HasPendingChanges = IsChanged();
                },
                () =>
                {
                    PreviewAdd(item, file);
                    HasPendingChanges = IsChanged();
                }));
        }

        private void PreviewAdd(MediaItemViewModel item, string path)
        {
            if (_removed.Contains(item))
            {
                _removed.Remove(item);
            }
            else
            {
                FileStream fileStream;

                try
                {
                    fileStream = File.OpenRead(path);
                }
                catch (Exception exc)
                {
                    OnError(exc);
                    return;
                }

                _added.Add(item);
                _streams[item] = Tuple.Create(path, fileStream);
            }

            Files.Add(item);
            OnPropertyChanged(nameof(Files));

            HasPendingChanges = IsChanged();
        }

        internal IMedia Wrap(string link)
        {
            var p = _streams.FirstOrDefault(n => n.Key.Model.Name == link);

            if (p.Key != null)
            {
                return new Media(p.Value.Item1);
            }

            return _document.Lock.WithLock(
                () =>
                {
                    var collection = _document.GetCollection(_name);

                    return PlatformSpecific.PlatformManager.Instance.PrepareMedia(
                        new Media(() => collection.GetFile(link), () => collection.GetFileLength(link), link),
                        collection.Name);
                });
        }
    }
}
