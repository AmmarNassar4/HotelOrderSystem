package com.ibaapps.HotelOrderSystem.di

import com.ibaapps.HotelOrderSystem.data.repository.AuthRepositoryImpl
import com.ibaapps.HotelOrderSystem.data.repository.DeviceRepositoryImpl
import com.ibaapps.HotelOrderSystem.data.repository.OrderRepositoryImpl
import com.ibaapps.HotelOrderSystem.data.repository.PresenceRepositoryImpl
import com.ibaapps.HotelOrderSystem.domain.repository.AuthRepository
import com.ibaapps.HotelOrderSystem.domain.repository.DeviceRepository
import com.ibaapps.HotelOrderSystem.domain.repository.OrderRepository
import com.ibaapps.HotelOrderSystem.domain.repository.PresenceRepository
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
abstract class RepositoryModule {

    @Binds
    @Singleton
    abstract fun bindAuthRepository(impl: AuthRepositoryImpl): AuthRepository

    @Binds
    @Singleton
    abstract fun bindDeviceRepository(impl: DeviceRepositoryImpl): DeviceRepository

    @Binds
    @Singleton
    abstract fun bindOrderRepository(impl: OrderRepositoryImpl): OrderRepository

    @Binds
    @Singleton
    abstract fun bindPresenceRepository(impl: PresenceRepositoryImpl): PresenceRepository
}
