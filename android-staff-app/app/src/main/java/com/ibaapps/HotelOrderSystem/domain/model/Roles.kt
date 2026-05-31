package com.ibaapps.HotelOrderSystem.domain.model

/** Backend role names and the rule for who may use the staff app. */
object Roles {
    const val ADMIN = "Admin"
    const val SUPERVISOR = "Supervisor"
    const val STAFF = "Staff"

    /** The staff app is for operational staff and supervisors, not admins. */
    fun isStaffAppAllowed(role: String?): Boolean =
        role.equals(STAFF, ignoreCase = true) || role.equals(SUPERVISOR, ignoreCase = true)

    fun canCancelOrders(role: String?): Boolean =
        role.equals(ADMIN, ignoreCase = true) || role.equals(SUPERVISOR, ignoreCase = true)
}
