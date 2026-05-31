package com.ibaapps.HotelOrderSystem.ui.navigation

/** Top-level navigation routes. */
object Routes {
    const val SPLASH = "splash"
    const val LOGIN = "login"
    const val ACCESS_DENIED = "accessDenied"
    const val MAIN = "main"

    const val ORDER_DETAILS = "orderDetails/{orderId}"
    const val ARG_ORDER_ID = "orderId"
    fun orderDetails(orderId: Int) = "orderDetails/$orderId"
}
