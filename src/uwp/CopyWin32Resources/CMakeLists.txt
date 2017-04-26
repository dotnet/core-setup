add_executable(CopyWin32Resources
  CopyWin32Resources.cpp
  native.rc
)

target_link_libraries(CopyWin32Resources
  imagehlp.lib
)

install(FILES $<TARGET_FILE:CopyWin32Resources> DESTINATION .)