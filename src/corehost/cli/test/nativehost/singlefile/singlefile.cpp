#include <pal.h>

// Export a symbol indicating this is a single-file host with the runtime embedded.
SHARED_API bool DotNetRuntimeEmbeddedSingleFileHost;
bool DotNetRuntimeEmbeddedSingleFileHost = true;
