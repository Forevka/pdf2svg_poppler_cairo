#include "pch.h"

#include <vector>
#include <cstdlib>
#include <cstring>
#include <poppler/cpp/poppler-document.h>
#include <poppler/cpp/poppler-page.h>
#include <poppler/cpp/poppler-page-renderer.h>

#include <glib.h>                
#include <glib-object.h>         // for g_object_unref
#include <poppler/glib/poppler.h>
#include <cairo/cairo.h>
#include <cairo/cairo-svg.h>

extern "C" {

    __declspec(dllexport)
        unsigned char* pdf_page_to_svg_mem(
            const guint8* pdf_data,
            int           pdf_len,
            int           page_num,
            int* out_svg_len
        );
}
cairo_status_t write_cb(void* closure,
    const unsigned char* data,
    unsigned int len) {
    auto* buf = static_cast<std::vector<unsigned char>*>(closure);
    buf->insert(buf->end(), data, data + len);
    return CAIRO_STATUS_SUCCESS;
}

unsigned char* pdf_page_to_svg_mem(const unsigned char* pdf_data,
    int pdf_len,
    int page_num,
    int* out_len)
{
    GError* err = nullptr;

    GBytes* bytes = g_bytes_new(pdf_data, (gsize)pdf_len);

    // Load PDF from memory
    PopplerDocument* doc = poppler_document_new_from_bytes(
        bytes, nullptr,  &err);
    if (!doc) return nullptr;

    // Fetch page and size
    PopplerPage* page = poppler_document_get_page(doc, page_num);
    double width, height;
    poppler_page_get_size(page, &width, &height);

    // Render to SVG via Cairo stream
    std::vector<unsigned char> svg_buf;
    cairo_surface_t* surface =
        cairo_svg_surface_create_for_stream(write_cb, &svg_buf, width, height);
    cairo_t* cr = cairo_create(surface);

    poppler_page_render(page, cr);

    // Tear down
    cairo_destroy(cr);
    cairo_surface_destroy(surface);
    g_object_unref(doc);
    g_bytes_unref(bytes);

    // Copy out
    *out_len = static_cast<int>(svg_buf.size());
    unsigned char* out = static_cast<unsigned char*>(std::malloc(*out_len));
    std::memcpy(out, svg_buf.data(), *out_len);
    return out;
}