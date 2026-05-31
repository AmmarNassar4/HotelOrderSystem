package com.ibaapps.HotelOrderSystem.di

import com.ibaapps.HotelOrderSystem.data.realtime.SignalRManager
import com.ibaapps.HotelOrderSystem.domain.realtime.RealtimeService
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
abstract class RealtimeModule {

    @Binds
    @Singleton
    abstract fun bindRealtimeService(impl: SignalRManager): RealtimeService
}
