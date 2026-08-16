# Copyright (c) 2026 RedBolt and contributors.
# This work is licensed under the terms of the MIT license.
# For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

redbolt_add_native_binding_generation(
    TARGET redbolt-generate-xatlas-bindings
    SURFACE xatlas
    HEADER "${REDBOLT_XATLAS_PARSE_HEADER}"
    HEADER_INCLUDE_PATH "XAtlasAll.hpp"
    FILE_STEM "${REDBOLT_XATLAS_BINDINGS_STEM}"
    OUTPUT_DIR "${REDBOLT_XATLAS_GENERATED_DIR}"
    OUTPUT_METADATA "${REDBOLT_XATLAS_BINDINGS_JSON}"
    OUTPUT_CS "${REDBOLT_XATLAS_BINDINGS_CS}"
    OUTPUT_CPP "${REDBOLT_XATLAS_BINDINGS_CPP}"
    NATIVE_LIBRARY_NAME "${REDBOLT_XATLAS_NATIVE_LIBRARY_NAME}"
    INCLUDE_DIRS "${REDBOLT_XATLAS_INCLUDE_DIR}"
    OWNED_HEADERS "${REDBOLT_XATLAS_PARSE_HEADER}" "${REDBOLT_XATLAS_HEADER}")
