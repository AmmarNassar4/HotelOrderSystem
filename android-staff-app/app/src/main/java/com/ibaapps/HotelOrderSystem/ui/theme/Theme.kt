package com.ibaapps.HotelOrderSystem.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val LightColors = lightColorScheme(
    primary = BluePrimary,
    onPrimary = Color.White,
    primaryContainer = BluePrimaryDark,
    onPrimaryContainer = Color.White,
    background = SlateSurface,
    onBackground = SlateOnSurface,
    surface = Color.White,
    onSurface = SlateOnSurface,
    error = OfflineRed,
    onError = Color.White
)

private val DarkColors = darkColorScheme(
    primary = BluePrimary,
    onPrimary = Color.White,
    background = SlateDark,
    onBackground = Color.White,
    surface = Color(0xFF1E293B),
    onSurface = Color.White,
    error = OfflineRed,
    onError = Color.White
)

@Composable
fun HotelStaffTheme(
    useDarkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = if (useDarkTheme) DarkColors else LightColors,
        typography = AppTypography,
        content = content
    )
}
