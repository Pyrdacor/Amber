global using System.Text;

#pragma warning disable CS8981
global using word = System.UInt16;
global using dword = System.UInt32;
global using qword = System.UInt64;
#pragma warning restore CS8981

#if GLES
global using Silk.NET.OpenGLES;
#else
global using Silk.NET.OpenGL;
#endif
