cmake_minimum_required(VERSION 3.28)

project(RedBoltGlow VERSION 0.1.0 LANGUAGES C CXX)

include(GNUInstallDirs)

set(BUILD_SHARED_LIBS OFF CACHE BOOL "Build dependencies statically" FORCE)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(CMAKE_CXX_SCAN_FOR_MODULES OFF)

if(MINGW)
    set(CMAKE_CXX_LINK_LIBRARY_USING_WHOLE_ARCHIVE
        "LINKER:--whole-archive"
        "<LINK_ITEM>"
        "LINKER:--no-whole-archive")
    set(CMAKE_CXX_LINK_LIBRARY_USING_WHOLE_ARCHIVE_SUPPORTED TRUE)
endif()

set(REDBOLT_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../RedBolt" CACHE PATH "RedBolt source root")
set(REDBOLT_SHARPCPP_ROOT "${REDBOLT_ROOT}/extern/SharpCpp" CACHE PATH "SharpCpp source root")
set(REDBOLT_DEPENDENCY_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/extern" CACHE PATH "RedBolt.Glow dependency cache")
set(REDBOLT_GENERATED_ROOT "${CMAKE_BINARY_DIR}/generated")
set(REDBOLT_NATIVE_GENERATED_DIR "${REDBOLT_GENERATED_ROOT}/redbolt-glow-native")
set(REDBOLT_RUNTIME_OUTPUT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/build/bin/$<CONFIG>/linux-x64/extern/runtimes/linux-x64/native" CACHE PATH "Native runtime output")
set(REDBOLT_PDB_OUTPUT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/build/bin/$<CONFIG>/linux-x64/extern/pdb" CACHE PATH "Native PDB output")

set(REDBOLT_XATLAS_NATIVE_LIBRARY_NAME "RedBolt.Native.XAtlas" CACHE INTERNAL "xatlas native library name")
set(REDBOLT_EMBREE_NATIVE_LIBRARY_NAME "RedBolt.Native.Embree" CACHE INTERNAL "Embree native library name")
set(REDBOLT_XATLAS_GENERATED_DIR "${REDBOLT_NATIVE_GENERATED_DIR}/xatlas" CACHE INTERNAL "xatlas binding output")
set(REDBOLT_EMBREE_GENERATED_DIR "${REDBOLT_NATIVE_GENERATED_DIR}/embree" CACHE INTERNAL "Embree binding output")
set(REDBOLT_XATLAS_BINDINGS_STEM "RedBolt.Bindings.XAtlas" CACHE INTERNAL "xatlas binding stem")
set(REDBOLT_EMBREE_BINDINGS_STEM "RedBolt.Bindings.Embree" CACHE INTERNAL "Embree binding stem")
set(REDBOLT_XATLAS_BINDINGS_CS "${REDBOLT_XATLAS_GENERATED_DIR}/${REDBOLT_XATLAS_BINDINGS_STEM}.Bindings.g.cs" CACHE INTERNAL "xatlas managed bindings")
set(REDBOLT_XATLAS_BINDINGS_CPP "${REDBOLT_XATLAS_GENERATED_DIR}/${REDBOLT_XATLAS_BINDINGS_STEM}.Bindings.g.cpp" CACHE INTERNAL "xatlas native bindings")
set(REDBOLT_XATLAS_BINDINGS_JSON "${REDBOLT_XATLAS_GENERATED_DIR}/${REDBOLT_XATLAS_BINDINGS_STEM}.Bindings.json" CACHE INTERNAL "xatlas binding metadata")
set(REDBOLT_EMBREE_BINDINGS_CS "${REDBOLT_EMBREE_GENERATED_DIR}/${REDBOLT_EMBREE_BINDINGS_STEM}.Bindings.g.cs" CACHE INTERNAL "Embree managed bindings")
set(REDBOLT_EMBREE_BINDINGS_CPP "${REDBOLT_EMBREE_GENERATED_DIR}/${REDBOLT_EMBREE_BINDINGS_STEM}.Bindings.g.cpp" CACHE INTERNAL "Embree native bindings")
set(REDBOLT_EMBREE_BINDINGS_JSON "${REDBOLT_EMBREE_GENERATED_DIR}/${REDBOLT_EMBREE_BINDINGS_STEM}.Bindings.json" CACHE INTERNAL "Embree binding metadata")
set(REDBOLT_XATLAS_PARSE_HEADER "${REDBOLT_XATLAS_GENERATED_DIR}/XAtlasAll.hpp" CACHE INTERNAL "xatlas parse header")
set(REDBOLT_EMBREE_PARSE_HEADER "${REDBOLT_EMBREE_GENERATED_DIR}/EmbreeAll.hpp" CACHE INTERNAL "Embree parse header")

find_program(DOTNET_EXECUTABLE dotnet REQUIRED)
function(redbolt_require_path path description)
    if(NOT EXISTS "${path}")
        message(FATAL_ERROR "${description} does not exist: ${path}")
    endif()
endfunction()

list(PREPEND CMAKE_MODULE_PATH "${REDBOLT_ROOT}/cmake/Modules")
set(CPM_DONT_UPDATE_MODULE_PATH ON CACHE BOOL "Use RedBolt CMake modules" FORCE)
set(CPM_SOURCE_CACHE "${REDBOLT_DEPENDENCY_ROOT}" CACHE PATH "CPM source cache" FORCE)
unset(CPM_DIRECTORY CACHE)
unset(CPM_VERSION CACHE)
include("${REDBOLT_ROOT}/cmake/CPM.cmake")
set(FETCHCONTENT_BASE_DIR "${CMAKE_BINARY_DIR}/deps")

set(SHARPCPP_BUILD_E2E OFF CACHE BOOL "Build SharpCpp E2E" FORCE)
set(SHARPCPP_BUILD_RBFX OFF CACHE BOOL "Build SharpCpp rbfx bindings" FORCE)
add_subdirectory("${REDBOLT_SHARPCPP_ROOT}" "${CMAKE_BINARY_DIR}/deps/SharpCpp" EXCLUDE_FROM_ALL)
include(extern/XAtlas.cmake)
include(extern/Embree.cmake)

file(MAKE_DIRECTORY "${REDBOLT_XATLAS_GENERATED_DIR}" "${REDBOLT_EMBREE_GENERATED_DIR}")
file(CONFIGURE OUTPUT "${REDBOLT_XATLAS_PARSE_HEADER}" CONTENT [=[
#pragma once
#include <cstddef>
#include <xatlas.h>
]=] @ONLY)
file(CONFIGURE OUTPUT "${REDBOLT_EMBREE_PARSE_HEADER}" CONTENT [=[
#pragma once
#include <embree4/rtcore_ray.h>
#include <embree4/rtcore.h>
]=] @ONLY)

include("${REDBOLT_ROOT}/cmake/RedBoltSharpCpp.cmake")
add_subdirectory(src/RedBolt/Native/XAtlas)
add_subdirectory(src/RedBolt/Native/Embree)

function(redbolt_glow_add_native_wrapper target output_name generation_target source owned_library)
    add_library(${target} SHARED "${source}")
    add_dependencies(${target} ${generation_target})
    target_compile_definitions(${target} PRIVATE REDBOLT_NATIVE_BUILD)
    target_compile_options(${target} PRIVATE
        "$<$<CXX_COMPILER_ID:GNU>:-Wno-attributes>"
        "$<$<CXX_COMPILER_ID:Clang>:-Wno-attributes>")
    if(MINGW)
        target_compile_options(${target} PRIVATE -Wa,-mbig-obj -include cstdint)
    endif()
    target_link_libraries(${target} PRIVATE SharpCpp::NativeSupport ${owned_library})
    set_target_properties(${target} PROPERTIES
        OUTPUT_NAME "${output_name}"
        RUNTIME_OUTPUT_DIRECTORY "${REDBOLT_RUNTIME_OUTPUT_DIR}"
        LIBRARY_OUTPUT_DIRECTORY "${REDBOLT_RUNTIME_OUTPUT_DIR}"
        ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib/$<CONFIG>"
        BUILD_RPATH "$ORIGIN"
        INSTALL_RPATH "$ORIGIN"
        INSTALL_RPATH_USE_LINK_PATH OFF
        BUILD_WITH_INSTALL_RPATH TRUE
        C_VISIBILITY_PRESET default
        CXX_VISIBILITY_PRESET default
        VISIBILITY_INLINES_HIDDEN OFF
        WINDOWS_EXPORT_ALL_SYMBOLS ON)
    if(CMAKE_CONFIGURATION_TYPES)
        foreach(configuration IN LISTS CMAKE_CONFIGURATION_TYPES)
            string(TOUPPER "${configuration}" configuration_upper)
            set_target_properties(${target} PROPERTIES
                RUNTIME_OUTPUT_DIRECTORY_${configuration_upper} "${REDBOLT_RUNTIME_OUTPUT_DIR}"
                LIBRARY_OUTPUT_DIRECTORY_${configuration_upper} "${REDBOLT_RUNTIME_OUTPUT_DIR}")
        endforeach()
    endif()
    if(WIN32)
        set_target_properties(${target} PROPERTIES PREFIX "")
    endif()
endfunction()

redbolt_glow_add_native_wrapper(
    redbolt-glow-native-xatlas
    "${REDBOLT_XATLAS_NATIVE_LIBRARY_NAME}"
    redbolt-generate-xatlas-bindings
    "${REDBOLT_XATLAS_BINDINGS_CPP}"
    xatlas)
target_include_directories(redbolt-glow-native-xatlas PRIVATE "${REDBOLT_XATLAS_INCLUDE_DIR}")

redbolt_glow_add_native_wrapper(
    redbolt-glow-native-embree
    "${REDBOLT_EMBREE_NATIVE_LIBRARY_NAME}"
    redbolt-generate-embree-bindings
    "${REDBOLT_EMBREE_BINDINGS_CPP}"
    embree)
target_include_directories(redbolt-glow-native-embree PRIVATE
    "${REDBOLT_EMBREE_INCLUDE_DIR}"
    "${REDBOLT_EMBREE_BUILD_INCLUDE_DIR}")

file(GENERATE
    OUTPUT "${REDBOLT_GENERATED_ROOT}/native-modules/$<CONFIG>/XATLAS.json"
    CONTENT "{\n  \"module\": \"XATLAS\",\n  \"managedSelector\": \"RedBolt.Bindings.XAtlas\",\n  \"nativeLibrary\": \"RedBolt.Native.XAtlas\",\n  \"requires\": [],\n  \"requirementClosure\": [],\n  \"artifacts\": [\"$<TARGET_FILE_NAME:redbolt-glow-native-xatlas>\"]\n}\n")
file(GENERATE
    OUTPUT "${REDBOLT_GENERATED_ROOT}/native-modules/$<CONFIG>/EMBREE.json"
    CONTENT "{\n  \"module\": \"EMBREE\",\n  \"managedSelector\": \"RedBolt.Bindings.Embree\",\n  \"nativeLibrary\": \"RedBolt.Native.Embree\",\n  \"requires\": [],\n  \"requirementClosure\": [],\n  \"artifacts\": [\"$<TARGET_FILE_NAME:redbolt-glow-native-embree>\"]\n}\n")

add_custom_target(redbolt-glow-native-all DEPENDS
    redbolt-glow-native-xatlas
    redbolt-glow-native-embree)
