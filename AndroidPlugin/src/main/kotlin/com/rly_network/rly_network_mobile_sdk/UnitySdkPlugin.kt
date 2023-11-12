package com.rlynetworkmobilesdk

import android.os.Handler
import android.os.Looper
import com.rlynetworkmobilesdk.EncryptedSharedPreferencesHelper
import com.rlynetworkmobilesdk.MnemonicStorageHelper
import org.kethereum.bip39.generateMnemonic
import org.kethereum.bip39.validate
import org.kethereum.bip39.dirtyPhraseToMnemonicWords
import org.kethereum.bip39.toSeed
import org.kethereum.bip39.wordlists.WORDLIST_ENGLISH
import org.kethereum.bip32.toKey
import android.content.Context

class UnitySdkPlugin(private val context: Context) {


    private val mnemonicHelper = MnemonicStorageHelper(context)

    private val MNEMONIC_STORAGE_KEY = "BIP39_MNEMONIC"
    private val handler = Handler(Looper.getMainLooper())

    interface ResultCallback<T> {
        fun onSuccess(data: T)
        fun onError(error: String)
    }

    fun getBundleId(callback: ResultCallback<String>) {
        handler.post {
            callback.onSuccess(context.packageName)
        }
    }

    fun getMnemonic(callback: ResultCallback<String?>) {
        handler.post {
            mnemonicHelper.read(MNEMONIC_STORAGE_KEY) {mnemonic: String? ->
                callback.onSuccess(mnemonic)
            }
        }
    }

    fun generateNewMnemonic(callback: ResultCallback<String>) {
        handler.post {
            callback.onSuccess(generateMnemonic(192, WORDLIST_ENGLISH))
        }
    }

    fun saveMnemonic(mnemonic: String, saveToCloud: Boolean, rejectOnCloudSaveFailure: Boolean, callback: ResultCallback<Boolean>) {
        handler.post {
            if (!org.kethereum.bip39.model.MnemonicWords(mnemonic).validate(WORDLIST_ENGLISH)) {
                callback.onError("Mnemonic is not valid")
                return@post
            }
            mnemonicHelper.save(MNEMONIC_STORAGE_KEY, mnemonic, saveToCloud, rejectOnCloudSaveFailure, { ->
                callback.onSuccess(true);

            }, {message: String ->
                callback.onError(message)
            })
        }
    }

    fun deleteMnemonic(callback: ResultCallback<Boolean>) {
        handler.post {
            mnemonicHelper.delete(MNEMONIC_STORAGE_KEY)
            callback.onSuccess(true)
        }
    }

    fun getPrivateKeyFromMnemonic(mnemonic: String, callback: ResultCallback<String>) {
        handler.post {
            if (!org.kethereum.bip39.model.MnemonicWords(mnemonic).validate(WORDLIST_ENGLISH)) {
                callback.onError("Mnemonic failed to pass check")
                return@post
            }

            val words = dirtyPhraseToMnemonicWords(mnemonic)
            val seed = words.toSeed()
            val key = seed.toKey("m/44'/60'/0'/0/0")

            val privateKey = key.keyPair.privateKey.key.toByteArray()

            callback.onSuccess(privateKey.map { it.toInt() and 0xFF }.joinToString("") { "%02x".format(it) })
        }
    }
}