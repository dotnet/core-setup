add_executable(UWPShim
  uwpshim.cpp
  native.rc
)

target_link_libraries(UWPShim
    UWPHost
)

install(TARGETS UWPShim DESTINATION .)
