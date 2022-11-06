﻿using IceBloc.InternalFormats;

namespace IceBloc.Export;

/// <summary>
/// Handles mesh export to more a common CG format.
/// </summary>
public interface IModelExporter
{
    /// <summary>
    /// Exports a <see cref="InternalMesh"/> and saves it to a given folder.
    /// </summary>
    public void Export(InternalMesh mesh, string path);
}
