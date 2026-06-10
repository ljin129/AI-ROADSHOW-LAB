<template>
	<div class="combat-page" :class="[isPc ? 'pc_page_container' : '']">
		<div class="combat-main" :class="[isPc ? 'pc_container' : '']">
			<roadshow-empty
				v-if="loaded && !loading && !activity.activityId"
				title="未找到活动信息"
				description="请返回任务书重新进入。"
			/>

			<section v-else class="combat-container">
				<div class="combat-countdown">
					<div v-if="hasCurrentAiRoleProfile" class="combat-topbar-main">
						<div class="combat-role-profile">
							<div class="combat-role-photo-wrap">
								<img
									v-if="currentAiRolePhotoUrl"
									class="combat-role-photo"
									:src="currentAiRolePhotoUrl"
									:alt="currentAiRoleName"
								>
								<div v-else class="combat-role-photo combat-role-photo-fallback">
									{{ currentAiRoleAvatar }}
								</div>
							</div>
							<div class="combat-role-copy">
								<div class="combat-role-label">当前对话客户</div>
								<div class="combat-role-name">{{ currentAiRoleName }}</div>
							</div>
						</div>
					</div>
					<div class="timer-chip" :class="{ compact: countdownParts.length === 2 }" :aria-label="`剩余时间 ${countdownText}`">
						<template v-for="(part, index) in countdownParts" :key="`${part}-${index}`">
							<span class="timer-chip-part">{{ part }}</span>
							<span v-if="index < countdownParts.length - 1" class="timer-chip-separator">:</span>
						</template>
					</div>
				</div>

				<div class="chat-window">
					<div ref="chatScrollerRef" class="chat-scroller">
						<div v-for="item in chatStream" :key="item.id" class="chat-node">
							<div v-if="item.type === 'status'" class="status-bubble">
								{{ item.content }}
							</div>

							<div v-else class="chat-item" :class="item.role">
								<div class="avatar-circle" :class="[item.role, { 'has-photo': !!getChatAvatarPhotoUrl(item) }]">
									<img
										v-if="getChatAvatarPhotoUrl(item)"
										class="avatar-photo"
										:src="getChatAvatarPhotoUrl(item)"
										:alt="getChatAvatarName(item)"
									>
									<span v-else class="avatar-text">{{ getChatAvatarText(item) }}</span>
								</div>
								<div class="bubble-wrap">
									<div class="message-bubble" :class="[item.role, item.pending ? 'pending' : '', (item.role === 'student' && !item.voiceFileId) ? 'no-voice' : '']">
										<button
											v-if="item.role === 'ai' || (item.role === 'student' && item.voiceFileId)"
											type="button"
											class="voice-icon-btn"
											:class="{ active: isAiSpeechActive(item) || activeRecordMessageId === item.id }"
											@click="item.role === 'ai' ? toggleAiSpeech(item) : toggleStudentSpeech(item)"
										>
											<span class="voice-icon-mark" aria-hidden="true">
												<span class="voice-arc arc-1"></span>
												<span class="voice-arc arc-2"></span>
												<span class="voice-arc arc-3"></span>
											</span>
										</button>
										<div class="message-content">{{ item.content }}</div>
									</div>
								</div>
							</div>
						</div>

						<div v-if="showAiLoadingBubble" class="chat-item ai">
							<div class="avatar-circle ai" :class="{ 'has-photo': !!currentAiRolePhotoUrl }">
								<img
									v-if="currentAiRolePhotoUrl"
									class="avatar-photo"
									:src="currentAiRolePhotoUrl"
									:alt="currentAiRoleName"
								>
								<span v-else class="avatar-text">{{ currentAiRoleAvatar }}</span>
							</div>
							<div class="bubble-wrap">
								<div class="message-bubble ai loading-bubble">
									<span class="loading-dot"></span>
									<span class="loading-dot"></span>
									<span class="loading-dot"></span>
								</div>
							</div>
						</div>

						<div v-if="showRecordingPreview" class="chat-item student">
							<div class="avatar-circle student">
								<span class="avatar-text">我</span>
							</div>
							<div class="bubble-wrap">
								<div class="sender-name">我</div>
								<div class="message-bubble student pending">
									<template v-if="currentSpeechText">
										<div class="message-content">{{ currentSpeechText }}</div>
									</template>
									<template v-else>
										<div class="loading-bubble loading-bubble-student">
											<span class="loading-dot loading-dot-light"></span>
											<span class="loading-dot loading-dot-light"></span>
											<span class="loading-dot loading-dot-light"></span>
										</div>
									</template>
								</div>
							</div>
						</div>
					</div>

					<div class="talk-dock-panel">
						<div class="talk-dock-shell">
							<button
								type="button"
								class="input-mode-btn"
								:class="toggleModeClass"
								:disabled="inputModeToggleDisabled"
								@click="toggleInputMode"
							>
								<span class="input-mode-icon" :class="toggleModeClass" aria-hidden="true"></span>
								<span class="input-mode-label">{{ toggleModeLabel }}</span>
							</button>

							<div class="talk-input-body">
								<button
									v-if="!isTextMode"
									type="button"
									class="h5-mic-btn"
									:class="isRecording ? 'recording' : 'idle'"
									:disabled="micDisabled"
									@click="handleMicButtonClick"
								>
									{{ buttonText }}
								</button>

								<div v-else class="text-composer" :class="textInputDisabled ? 'disabled' : ''">
									<textarea
										ref="textComposerFieldRef"
										v-model="textDraft"
										class="text-composer-field"
										rows="1"
										:maxlength="textMaxLength"
										:disabled="textInputDisabled"
										:placeholder="textPlaceholder"
										@input="handleTextDraftInput"
									></textarea>
									<button
										type="button"
										class="text-send-btn"
										:disabled="!canSendText"
										@click="handleTextSubmit"
									>
										发送
									</button>
								</div>
							</div>
						</div>
					</div>
				</div>
			</section>
		</div>
	</div>
</template>

<script>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, toRefs, watch } from 'vue'
import { useRoute } from 'vue-router'
import { Toast } from 'vant'
import RoadshowEmpty from './components/roadshowEmpty.vue'
import {
	GetActivityDetail,
	GetPracticeDetail,
	GetRoadshowAsrProvider,
	OpenPracticeChatStream,
} from '@/api/activity'
import { CreatePlayer } from '@/utils/SAPlayer'
import { MiniProgramRecorder } from './roadshow/miniProgramRecorder'
import { TtsPlayer } from './roadshow/ttsPlayer'

const MIN_RECORD_MS = 800
const DEFAULT_COMBAT_SECONDS = 30 * 60
const RECORD_RESULT_TIMEOUT = 12000
const TEXT_MAX_LENGTH = 300

const createAudioContext = () => {
	const audioContext = new (window.AudioContext || window.webkitAudioContext)()
	if (audioContext && typeof audioContext.resume === 'function') {
		audioContext.resume().catch(() => {})
	}
	return audioContext
}

const formatSeconds = (seconds = 0) => {
	const safeSeconds = Math.max(0, Number(seconds || 0))
	const hours = Math.floor(safeSeconds / 3600)
	const minutes = Math.floor((safeSeconds % 3600) / 60)
	const remainSeconds = safeSeconds % 60
	const prefix = hours > 0 ? `${String(hours).padStart(2, '0')}:` : ''
	return `${prefix}${String(minutes).padStart(2, '0')}:${String(remainSeconds).padStart(2, '0')}`
}

const readQueryValue = (sdk, route, key) => {
	const lower = route.query[key]
	const upper = route.query[key.charAt(0).toUpperCase() + key.slice(1)]
	if (lower !== undefined && lower !== null && `${lower}`.trim()) return `${lower}`.trim()
	if (upper !== undefined && upper !== null && `${upper}`.trim()) return `${upper}`.trim()
	if (sdk && typeof sdk.query === 'function') {
		return `${sdk.query(key) || sdk.query(key.charAt(0).toUpperCase() + key.slice(1)) || ''}`.trim()
	}
	return ''
}

const getResponseData = (payload) => {
	if (!payload || typeof payload !== 'object') return null
	return payload.Data !== undefined ? payload.Data : payload.data
}

const toText = (value) => {
	if (value === null || value === undefined) return ''
	return `${value}`.trim()
}

const createTempAiTtsKey = (practiceRecordId = '') => {
	const scope = Number(practiceRecordId) || 0
	return scope + Date.now()
}

const normalizeActivity = (item = {}) => ({
	activityId: toText(item.activityId ?? item.ActivityId),
	activityName: toText(item.activityName ?? item.ActivityName),
	activityDesc: toText(item.activityDesc ?? item.ActivityDesc),
	coreGoal: toText(item.coreGoal ?? item.CoreGoal),
	customerBackground: toText(item.customerBackground ?? item.CustomerBackground),
	recommendedDurationMinutes: Number(item.recommendedDurationMinutes ?? item.RecommendedDurationMinutes ?? 0) || 0,
})

const normalizeRoleProfile = (item = {}) => ({
	roleId: toText(item.roleId ?? item.RoleId),
	questionId: toText(item.questionId ?? item.QuestionId),
	roleNickname: toText(item.roleNickname ?? item.RoleNickname),
	custId: toText(item.custId ?? item.CustId),
	photoUrl: toText(item.photoUrl ?? item.PhotoUrl),
	stageId: toText(item.stageId ?? item.StageId),
	stageName: toText(item.stageName ?? item.StageName),
})

const createEmptyRoleProfile = () => ({
	roleId: '',
	questionId: '',
	roleNickname: '',
	custId: '',
	photoUrl: '',
	stageId: '',
	stageName: '',
})

const buildAvatar = (name) => {
	const text = toText(name)
	return text ? Array.from(text)[0] : '问'
}

const shouldHideCompletedAiMessage = (content = '', isCompleted = false) => {
	if (!isCompleted) return false
	const text = toText(content).replace(/[\s。！!；;，,]/g, '')
	return text === '本次路演演练已完成' || text === '本轮实战演练已完成' || text === '本次路演已完成'
}

export default {
	name: 'ActivityCombat',
	components: {
		RoadshowEmpty,
	},
	setup() {
		const route = useRoute()
		const sdk = window.bjsdk || {}
		const isPc = ref((sdk.isWinPC || sdk.isMac) && !sdk.isWM)
		const chatScrollerRef = ref(null)
		const textComposerFieldRef = ref(null)
		const streamController = ref(null)
		const countdownTimer = ref(null)
		const resultMonitorTimer = ref(null)
		const recorder = ref(null)
		const ttsPlayer = ref(null)
		const recordMessagePlayer = ref(null)
		const typewriterTimer = ref(null)
		const activeSpeechMessageId = ref('')
		const activeRecordMessageId = ref('')
		const pendingAiMessageId = ref('')
		const recordStartedAt = ref(0)
		const state = reactive({
			loading: false,
			loaded: false,
			activity: {},
			chatStream: [],
			currentAiRoleProfile: createEmptyRoleProfile(),
			combatTimer: DEFAULT_COMBAT_SECONDS,
			talkTip: '点击开始录音，完成后点击停止录音并发送。',
			sampleRate: '00',
			practiceRecordId: readQueryValue(sdk, route, 'practiceRecordId'),
			currentDetailId: '',
			isRecording: false,
			isRecorderReady: false,
			isSubmitting: false,
			isStreaming: false,
			isTranscribing: false,
			isFinished: false,
			currentSpeechText: '',
			inputMode: 'voice',
			textDraft: '',
			speechSupported: false,
		})

		const activityId = computed(() => readQueryValue(sdk, route, 'activityId'))
		const countdownText = computed(() => formatSeconds(state.combatTimer))
		const countdownParts = computed(() => countdownText.value.split(':'))
		const hasCurrentAiRoleProfile = computed(() => {
			return !!(toText(state.currentAiRoleProfile.roleNickname) || toText(state.currentAiRoleProfile.photoUrl) || toText(state.currentAiRoleProfile.custId))
		})
		const currentAiRoleName = computed(() => toText(state.currentAiRoleProfile.roleNickname) || 'AI 客户')
		const currentAiRolePhotoUrl = computed(() => toText(state.currentAiRoleProfile.photoUrl))
		const currentAiRoleAvatar = computed(() => buildAvatar(currentAiRoleName.value))
		const showAiLoadingBubble = computed(() => {
			if (state.loaded && !state.loading && !state.activity.activityId) return false
			return (state.loading || state.isStreaming) && !pendingAiMessageId.value
		})
		const showRecordingPreview = computed(() => state.isRecording || state.isTranscribing)
		const isTextMode = computed(() => state.inputMode === 'text')
		const micDisabled = computed(() => {
			return state.loading || !state.practiceRecordId || state.isSubmitting || state.isStreaming || state.isFinished
		})
		const textInputDisabled = computed(() => {
			return state.loading || !state.practiceRecordId || state.isSubmitting || state.isStreaming || state.isFinished || state.isRecording || state.isTranscribing
		})
		const canSendText = computed(() => isTextMode.value && !textInputDisabled.value && !!toText(state.textDraft))
		const toggleModeClass = computed(() => (isTextMode.value ? 'voice-mode' : 'text-mode'))
		const toggleModeLabel = computed(() => (isTextMode.value ? '语音' : '文字'))
		const inputModeToggleDisabled = computed(() => {
			return state.loading || !state.practiceRecordId || state.isSubmitting || state.isStreaming || state.isRecording || state.isTranscribing || state.isFinished
		})
		const textPlaceholder = computed(() => {
			if (state.isFinished) return '本轮演练已结束'
			if (state.isStreaming) return 'AI 正在回复中'
			if (state.isSubmitting) return '正在发送中...'
			return '请输入你的回答'
		})
		const buttonText = computed(() => {
			if (state.isFinished) return '本轮演练已结束'
			if (state.isTranscribing) return '正在转写...'
			if (state.isStreaming) return 'AI 正在提问...'
			return state.isRecording ? '停止录音并发送' : '点击开始录音'
		})

		const scrollChatToBottom = () => {
			nextTick(() => {
				const element = chatScrollerRef.value
				if (element) {
					element.scrollTop = element.scrollHeight
				}
			})
		}

		const resizeTextComposerField = () => {
			nextTick(() => {
				const element = textComposerFieldRef.value
				if (!element) return
				element.style.height = 'auto'
				const nextHeight = Math.min(Math.max(element.scrollHeight, 28), 100)
				element.style.height = `${nextHeight}px`
				element.style.overflowY = element.scrollHeight > 100 ? 'auto' : 'hidden'
			})
		}

		const handleTextDraftInput = () => {
			resizeTextComposerField()
		}

		const clearResultMonitor = () => {
			if (resultMonitorTimer.value) {
				clearTimeout(resultMonitorTimer.value)
				resultMonitorTimer.value = null
			}
		}

		const clearTypewriterTimer = () => {
			if (typewriterTimer.value) {
				clearInterval(typewriterTimer.value)
				typewriterTimer.value = null
			}
		}

		const stopSpeechPlayback = () => {
			if (ttsPlayer.value) {
				ttsPlayer.value.stop()
			}
			activeSpeechMessageId.value = ''
		}

		const stopRecordMessagePlayback = () => {
			if (recordMessagePlayer.value) {
				recordMessagePlayer.value.stop()
			}
			activeRecordMessageId.value = ''
		}

		const initTtsPlayer = () => {
			if (ttsPlayer.value) {
				ttsPlayer.value.close()
				ttsPlayer.value = null
			}

			ttsPlayer.value = new TtsPlayer({
				sampleRate: state.sampleRate || '00',
				customerId: '',
				onStateChange: ({ key, supported, isPlaying }) => {
					activeSpeechMessageId.value = isPlaying ? (key || '') : ''
					state.speechSupported = !!supported
				},
			})
			state.speechSupported = ttsPlayer.value.isSupported()
		}

		const ensureRecordMessagePlayer = () => {
			if (recordMessagePlayer.value) return recordMessagePlayer.value
			recordMessagePlayer.value = CreatePlayer({
				ctxCreator: createAudioContext,
				mode: 1,
				onEnded: (target) => {
					if (target === activeRecordMessageId.value) {
						activeRecordMessageId.value = ''
					}
				},
			})
			return recordMessagePlayer.value
		}

		const getRecordAudioUrl = (fileId = '') => {
			const normalizedFileId = toText(fileId)
			if (!normalizedFileId) return ''
			if (typeof sdk.url === 'function') {
				return sdk.url(`//{ecdn}/Beta.FileService/f/${normalizedFileId}`, true)
			}
			return `${process.env.VUE_APP_CDN || ''}Beta.FileService/f/${normalizedFileId}`
		}

		const clearCountdownTimer = () => {
			if (countdownTimer.value) {
				clearInterval(countdownTimer.value)
				countdownTimer.value = null
			}
		}

		const appendStatusMessage = (content) => {
			const text = toText(content)
			if (!text) return
			state.chatStream.push({
				id: `status_${Date.now()}_${state.chatStream.length}`,
				type: 'status',
				content: text,
			})
			scrollChatToBottom()
		}

		const getRoleProfileFromChatItem = (item = {}) => normalizeRoleProfile({
			roleId: item.roleId,
			questionId: item.questionId,
			roleNickname: item.roleNickname || item.sender,
			custId: item.customerId,
			photoUrl: item.photoUrl,
			stageId: item.stageId,
			stageName: item.stageName,
		})

		const syncCurrentAiRoleProfile = (payload = {}) => {
			const nextProfile = normalizeRoleProfile(payload)
			if (!nextProfile.roleId && !nextProfile.roleNickname && !nextProfile.custId && !nextProfile.photoUrl) return

			state.currentAiRoleProfile = {
				...state.currentAiRoleProfile,
				...nextProfile,
			}

			if (!pendingAiMessageId.value) return

			const targetIndex = state.chatStream.findIndex((item) => item.id === pendingAiMessageId.value)
			if (targetIndex < 0) return

			const currentItem = state.chatStream[targetIndex]
			state.chatStream[targetIndex] = {
				...currentItem,
				roleId: nextProfile.roleId || currentItem.roleId,
				roleNickname: nextProfile.roleNickname || currentItem.roleNickname,
				customerId: nextProfile.custId || currentItem.customerId,
				photoUrl: nextProfile.photoUrl || currentItem.photoUrl,
				stageName: nextProfile.stageName || currentItem.stageName,
				sender: nextProfile.roleNickname || currentItem.sender,
				avatar: buildAvatar(nextProfile.roleNickname || currentItem.sender),
			}
		}

		const restoreCurrentAiRoleProfileFromHistory = () => {
			const lastAiMessage = [...state.chatStream].reverse().find((item) => {
				if (item.role !== 'ai') return false
				return !!(toText(item.roleNickname) || toText(item.customerId) || toText(item.photoUrl))
			})
			if (!lastAiMessage) return
			syncCurrentAiRoleProfile(getRoleProfileFromChatItem(lastAiMessage))
		}

		const buildChatItem = ({
			id = '',
			detailId = '',
			ttsKey = '',
			streamTtsKey = '',
			role = 'ai',
			roleId = '',
			roleNickname = '',
			customerId = '',
			photoUrl = '',
			questionId = '',
			stageId = '',
			sender = '',
			content = '',
			voiceFileId = '',
			type = 'message',
			stageName = '',
			isFollowUp = false,
			pending = false,
		} = {}) => ({
			id: toText(id) || `chat_${Date.now()}_${Math.random().toString(16).slice(2, 8)}`,
			detailId: toText(detailId) || toText(id),
			ttsKey: toText(ttsKey) || toText(detailId) || toText(id),
			streamTtsKey: toText(streamTtsKey) || toText(ttsKey) || toText(detailId) || toText(id),
			type,
			role,
			roleId: toText(roleId),
			roleNickname: toText(roleNickname),
			customerId: toText(customerId),
			photoUrl: toText(photoUrl),
			questionId: toText(questionId),
			stageId: toText(stageId),
			sender: sender || (role === 'ai' ? 'AI 客户' : '我'),
			avatar: buildAvatar(sender || (role === 'ai' ? 'AI 客户' : '我')),
			content: role === 'ai' ? '' : toText(content),
			rawContent: role === 'ai' ? toText(content) : toText(content),
			voiceFileId: toText(voiceFileId),
			stageName: toText(stageName),
			isFollowUp: !!isFollowUp,
			pending: !!pending,
		})

		const isAiContentType = (contentType) => {
			const t = toText(contentType).toLowerCase()
			return t === 'ai' || t === 'ai_followup'
		}

		const mapDetailToChatItem = (detail) => {
			const isAi = isAiContentType(detail.ContentType || detail.contentType)
			const content = toText(detail.Content ?? detail.content)
			const sender = isAi ? 'AI 客户' : '我'
			return {
				id: toText(detail.DetailId ?? detail.detailId) || `chat_${Date.now()}_${Math.random().toString(16).slice(2, 8)}`,
				detailId: toText(detail.DetailId ?? detail.detailId),
				ttsKey: toText(detail.DetailId ?? detail.detailId),
				streamTtsKey: toText(detail.DetailId ?? detail.detailId),
				type: 'message',
				role: isAi ? 'ai' : 'student',
				roleId: toText(detail.RoleId ?? detail.roleId),
				roleNickname: toText(detail.RoleNickname ?? detail.roleNickname),
				customerId: toText(detail.CustId ?? detail.custId),
				photoUrl: toText(detail.PhotoUrl ?? detail.photoUrl),
				questionId: toText(detail.QuestionId ?? detail.questionId),
				sender,
				avatar: buildAvatar(sender),
				content,
				rawContent: content,
				voiceFileId: toText(detail.DialogVoiceFileId ?? detail.dialogVoiceFileId),
				isFollowUp: !!(detail.IsFollowUp ?? detail.isFollowUp),
				pending: false,
			}
		}

		const computeRecoveredTimer = (details, totalSeconds) => {
			if (!Array.isArray(details) || details.length < 2) return totalSeconds

			const firstTime = new Date(details[0].CreateTime ?? details[0].createTime).getTime()
			const lastTime = new Date(details[details.length - 1].CreateTime ?? details[details.length - 1].createTime).getTime()

			if (isNaN(firstTime) || isNaN(lastTime) || lastTime <= firstTime) return totalSeconds

			const elapsedSeconds = Math.floor((lastTime - firstTime) / 1000)
			return Math.max(0, totalSeconds - elapsedSeconds)
		}

		const startTypewriter = () => {
			if (typewriterTimer.value) return

			typewriterTimer.value = window.setInterval(() => {
				const targetIndex = state.chatStream.findIndex((item) => {
					if (item.role !== 'ai') return false
					return toText(item.rawContent).length > toText(item.content).length
				})

				if (targetIndex < 0) {
					clearTypewriterTimer()
					return
				}

				const currentItem = state.chatStream[targetIndex]
				const source = toText(currentItem.rawContent)
				const displayed = toText(currentItem.content)
				const step = Math.min(3, source.length - displayed.length)
				state.chatStream[targetIndex] = {
					...currentItem,
					content: `${displayed}${source.slice(displayed.length, displayed.length + step)}`,
				}
				scrollChatToBottom()
			}, 28)
		}

		const getPlayableAiText = (item) => {
			if (!item || item.role !== 'ai') return ''
			return toText(item.rawContent) || toText(item.content)
		}

		const getAiStreamTtsKey = (item) => {
			if (!item || item.role !== 'ai') return ''
			return toText(item.streamTtsKey) || toText(item.ttsKey) || toText(item.detailId) || toText(item.id)
		}

		const getAiReplayTtsKey = (item) => {
			if (!item || item.role !== 'ai') return ''
			return toText(item.detailId) || toText(item.ttsKey) || toText(item.id)
		}

		const getAiCustomerId = (item) => {
			if (!item || item.role !== 'ai') return ''
			return toText(item.customerId) || toText(state.currentAiRoleProfile.custId)
		}

		const getChatAvatarPhotoUrl = (item) => {
			if (!item) return ''
			if (item.role === 'ai') {
				return toText(item.photoUrl) || currentAiRolePhotoUrl.value
			}
			return ''
		}

		const getChatAvatarName = (item) => {
			if (!item) return ''
			if (item.role === 'ai') {
				return toText(item.roleNickname) || toText(item.sender) || currentAiRoleName.value
			}
			return toText(item.sender) || '我'
		}

		const getChatAvatarText = (item) => buildAvatar(getChatAvatarName(item))

		const isAiSpeechActive = (item) => {
			if (!item || item.role !== 'ai') return false
			const activeKey = toText(activeSpeechMessageId.value)
			if (!activeKey) return false
			return [
				toText(item.id),
				toText(item.detailId),
				toText(item.ttsKey),
				toText(item.streamTtsKey),
				getAiReplayTtsKey(item),
				getAiStreamTtsKey(item),
			].filter(Boolean).includes(activeKey)
		}

		const upsertPendingAiMessage = (deltaText) => {
			const nextDelta = toText(deltaText)
			if (!nextDelta) return

			if (!pendingAiMessageId.value) {
				const tempTtsKey = createTempAiTtsKey(state.practiceRecordId)
				const activeRoleProfile = normalizeRoleProfile(state.currentAiRoleProfile)
				const message = buildChatItem({
					id: tempTtsKey,
					detailId: '',
					ttsKey: tempTtsKey,
					streamTtsKey: tempTtsKey,
					role: 'ai',
					roleId: activeRoleProfile.roleId,
					roleNickname: activeRoleProfile.roleNickname,
					customerId: activeRoleProfile.custId,
					photoUrl: activeRoleProfile.photoUrl,
					questionId: activeRoleProfile.questionId,
					stageId: activeRoleProfile.stageId,
					sender: 'AI 客户',
					content: nextDelta,
					pending: true,
				})
				pendingAiMessageId.value = message.id
				state.chatStream.push(message)
				pushAiTextToTts(message, false)
				startTypewriter()
				scrollChatToBottom()
				return
			}

			const targetIndex = state.chatStream.findIndex((item) => item.id === pendingAiMessageId.value)
			if (targetIndex < 0) {
				pendingAiMessageId.value = ''
				upsertPendingAiMessage(nextDelta)
				return
			}

			const currentItem = state.chatStream[targetIndex]
			const nextItem = {
				...currentItem,
				rawContent: `${toText(currentItem.rawContent)}${nextDelta}`,
			}
			state.chatStream[targetIndex] = nextItem
			pushAiTextToTts(nextItem, false)
			startTypewriter()
			scrollChatToBottom()
		}

		const finalizeAiMessage = (payload) => {
			const content = toText(payload && payload.Content !== undefined ? payload.Content : payload && payload.content)
			const detailId = toText(payload && (payload.DetailId !== undefined ? payload.DetailId : payload.detailId))
			const activeRoleProfile = normalizeRoleProfile(state.currentAiRoleProfile)
			const roleId = toText(payload && (payload.RoleId !== undefined ? payload.RoleId : payload.roleId)) || activeRoleProfile.roleId
			const customerId = toText(payload && (payload.CustId !== undefined ? payload.CustId : payload.custId)) || activeRoleProfile.custId
			const photoUrl = toText(payload && (payload.PhotoUrl !== undefined ? payload.PhotoUrl : payload.photoUrl)) || activeRoleProfile.photoUrl
			const questionId = toText(payload && (payload.QuestionId !== undefined ? payload.QuestionId : payload.questionId)) || activeRoleProfile.questionId
			const stageId = toText(payload && (payload.StageId !== undefined ? payload.StageId : payload.stageId)) || activeRoleProfile.stageId
			const roleNickname = toText(payload && (payload.RoleNickname !== undefined ? payload.RoleNickname : payload.roleNickname)) || 'AI 客户'
			const stageName = toText(payload && (payload.StageName !== undefined ? payload.StageName : payload.stageName))
			const isFollowUp = !!(payload && (payload.IsFollowUp !== undefined ? payload.IsFollowUp : payload.isFollowUp))
			const isCompleted = !!(payload && (payload.IsCompleted !== undefined ? payload.IsCompleted : payload.isCompleted))
			const streamTtsKey = pendingAiMessageId.value || detailId || ''
			const replayTtsKey = detailId || pendingAiMessageId.value || ''
			const hideCompletedMessage = shouldHideCompletedAiMessage(content, isCompleted)

			const nextMessage = buildChatItem({
				id: detailId || pendingAiMessageId.value || '',
				detailId,
				ttsKey: replayTtsKey,
				streamTtsKey,
				role: 'ai',
				roleId,
				roleNickname,
				customerId,
				photoUrl,
				questionId,
				stageId,
				sender: roleNickname,
				content,
				stageName,
				isFollowUp,
				pending: false,
			})
			let finalizedMessage = nextMessage

			if (pendingAiMessageId.value) {
				const targetIndex = state.chatStream.findIndex((item) => item.id === pendingAiMessageId.value)
				if (targetIndex >= 0) {
					if (hideCompletedMessage) {
						state.chatStream.splice(targetIndex, 1)
						finalizedMessage = null
					} else {
						const currentItem = state.chatStream[targetIndex]
						finalizedMessage = {
							...nextMessage,
							id: currentItem.id,
							detailId: detailId || currentItem.detailId,
							ttsKey: detailId || currentItem.ttsKey || nextMessage.id,
							streamTtsKey: currentItem.streamTtsKey || currentItem.ttsKey || detailId || nextMessage.streamTtsKey,
							content: toText(currentItem.content),
							rawContent: content || toText(currentItem.rawContent),
						}
						state.chatStream[targetIndex] = finalizedMessage
					}
				} else {
					if (hideCompletedMessage) {
						finalizedMessage = null
					} else {
						finalizedMessage = nextMessage
						state.chatStream.push(finalizedMessage)
					}
				}
			} else {
				if (hideCompletedMessage) {
					finalizedMessage = null
				} else {
					finalizedMessage = nextMessage
					state.chatStream.push(finalizedMessage)
				}
			}

			pendingAiMessageId.value = ''
			if (detailId && !isCompleted) {
				state.currentDetailId = detailId
			}
			if (isCompleted) {
				state.isFinished = true
				state.talkTip = '本轮实战演练已完成。'
			}

			if (finalizedMessage) {
				syncCurrentAiRoleProfile(getRoleProfileFromChatItem(finalizedMessage))
				pushAiTextToTts(finalizedMessage, true)
				startTypewriter()
			}
			scrollChatToBottom()
		}

		const pushAiTextToTts = (item, isEnd) => {
			const text = getPlayableAiText(item)
			if (!ttsPlayer.value || !item || item.role !== 'ai' || !text) return
			const ttsKey = getAiStreamTtsKey(item)
			const customerId = getAiCustomerId(item)
			ttsPlayer.value.send(text, ttsKey, {
				channelId: ttsKey,
				customerId,
				isStream: true,
				isEnd,
				isClick: false,
			})
		}

		const handleRoleProfileEvent = (payload) => {
			syncCurrentAiRoleProfile(payload || {})
		}

		const handlePracticeStatus = (payload) => {
			const step = toText(payload && payload.step)
			if (!step) return

			switch (step) {
				case 'switch_stage':
					break
				case 'generate_followup':
					appendStatusMessage('AI 正在根据您的回答生成追问。')
					break
				case 'completed':
					appendStatusMessage('本次路演已完成')
					break
				default:
					break
			}
		}

		const handlePracticeEvent = ({ event, data }) => {
			switch (event) {
				case 'status':
					handlePracticeStatus(data)
					break
				case 'role_profile':
					handleRoleProfileEvent(data)
					break
				case 'delta':
					upsertPendingAiMessage(data && (data.content !== undefined ? data.content : data.Content))
					break
				case 'message':
					finalizeAiMessage(data || {})
					break
				case 'done':
					state.isSubmitting = false
					state.isStreaming = false
					if (!state.isFinished) {
						state.talkTip = '点击开始录音，完成后点击停止录音并发送。'
					}
					break
				case 'error':
					state.isSubmitting = false
					state.isStreaming = false
					pendingAiMessageId.value = ''
					Toast.fail(toText(data && data.message) || '路演对话失败，请稍后重试')
					state.talkTip = '路演对话异常，请重新发起录音。'
					break
				default:
					break
			}
		}

		const handlePracticeStreamError = (error) => {
			state.isSubmitting = false
			state.isStreaming = false
			pendingAiMessageId.value = ''
			Toast.fail(toText(error && error.message) || '路演对话失败，请稍后重试')
			state.talkTip = '路演对话异常，请重新发起录音。'
		}

		const openPracticeTurn = async ({
			answerContent = null,
			answerVoiceFileId = null,
		} = {}) => {
			if (!state.practiceRecordId || !activityId.value) {
				throw new Error('演练上下文不完整，无法继续对话')
			}

			if (streamController.value) {
				streamController.value.abort()
				streamController.value = null
			}

			state.isSubmitting = true
			state.isStreaming = true
			state.talkTip = answerContent ? '已收到您的回答，AI 客户正在继续对话...' : 'AI 客户正在生成首轮问题...'

			scrollChatToBottom()
			const streamSession = OpenPracticeChatStream({
				payload: {
					activityId: activityId.value,
					practiceRecordId: state.practiceRecordId,
					currentDetailId: state.currentDetailId || null,
					aiReplyVoiceFileId: null,
					answerVoiceFileId: answerVoiceFileId || null,
					answerContent: answerContent || null,
				},
				onEvent: handlePracticeEvent,
				onError: handlePracticeStreamError,
			})

			streamController.value = streamSession.controller
			await streamSession.finished.finally(() => {
				state.isSubmitting = false
				state.isStreaming = false
				if (streamController.value === streamSession.controller) {
					streamController.value = null
				}
			})
		}

		const resetRecordState = () => {
			clearResultMonitor()
			state.isRecording = false
			state.isTranscribing = false
			state.currentSpeechText = ''
		}

		const submitStudentAnswer = async ({ text = '', fileId = '', source = 'voice' } = {}) => {
			const answerText = toText(text)
			if (!answerText) {
				if (source === 'text') {
					Toast.fail('请输入回答内容')
					return
				}
				resetRecordState()
				Toast.fail('未识别到有效语音内容，请重新录音')
				state.talkTip = '点击开始录音，完成后点击停止录音并发送。'
				return
			}

			stopSpeechPlayback()
			stopRecordMessagePlayback()
			state.chatStream.push(buildChatItem({
				role: 'student',
				sender: '我',
				content: answerText,
				voiceFileId: fileId,
			}))
			scrollChatToBottom()

			state.isRecording = false
			state.isTranscribing = false
			state.currentSpeechText = ''

			try {
				await openPracticeTurn({
					answerContent: answerText,
					answerVoiceFileId: fileId || null,
				})
			} catch (error) {
				if (source === 'text') {
					state.textDraft = answerText
				}
				Toast.fail(toText(error && error.message) || (source === 'text' ? '提交文字失败，请稍后重试' : '提交录音失败，请稍后重试'))
				state.isSubmitting = false
				state.isStreaming = false
				state.talkTip = source === 'text' ? '提交失败，请重新输入文字。' : '提交失败，请重新录音。'
			}
		}

		const handleRecorderError = (error) => {
			resetRecordState()
			Toast.fail(toText(error && error.message) || '语音转文字失败，请重新录音')
			state.talkTip = '点击开始录音，完成后点击停止录音并发送。'
		}

		const initRecorder = () => {
			const recordId = toText(state.practiceRecordId)
			if (!recordId) return
			if (recorder.value) {
				recorder.value.close()
			}

			recorder.value = new MiniProgramRecorder({
				recordId,
				sampleRate: state.sampleRate || '00',
				onRecordingStart: () => {
					state.isRecorderReady = true
					state.isRecording = true
					state.isTranscribing = false
					state.currentSpeechText = ''
					state.talkTip = '正在录音，请自然回答客户问题。'
					recordStartedAt.value = Date.now()
					scrollChatToBottom()
				},
				onPartial: (text) => {
					state.currentSpeechText = `${state.currentSpeechText}${toText(text)}`
					scrollChatToBottom()
				},
				onResult: ({ fileId, text }) => {
					clearResultMonitor()
					submitStudentAnswer({ text, fileId, source: 'voice' })
				},
				onError: handleRecorderError,
			})
		}

		const startRecord = () => {
			if (micDisabled.value || !recorder.value) {
				if (!toText(state.practiceRecordId)) {
					Toast.fail('缺少语音通道标识，暂时无法录音')
				}
				return
			}

			stopSpeechPlayback()
			stopRecordMessagePlayback()
			state.isRecorderReady = false
			state.isRecording = true
			state.isTranscribing = false
			state.currentSpeechText = ''
			state.talkTip = '正在唤起小程序录音能力，请开始回答。'
			recordStartedAt.value = Date.now()
			scrollChatToBottom()
			recorder.value.start()
		}

		const stopRecord = () => {
			if (!recorder.value || !state.isRecording) return

			const duration = Date.now() - recordStartedAt.value
			if (duration < MIN_RECORD_MS) {
				recorder.value.cancel()
				resetRecordState()
				Toast.fail('录音时间太短，请重新录音')
				state.talkTip = '点击开始录音，完成后点击停止录音并发送。'
				return
			}

			state.isRecording = false
			state.isTranscribing = true
			state.talkTip = '正在转写您的语音，请稍候。'
			recorder.value.stop()

			clearResultMonitor()
			resultMonitorTimer.value = setTimeout(() => {
				const fallbackResult = recorder.value ? recorder.value.consumeBufferedResult() : { text: '', fileId: '' }
				if (fallbackResult.text) {
					submitStudentAnswer({ ...fallbackResult, source: 'voice' })
					return
				}
				if (recorder.value) {
					recorder.value.cancel()
				}
				handleRecorderError(new Error('语音转文字超时，请重新录音'))
			}, RECORD_RESULT_TIMEOUT)
		}

		const handleMicButtonClick = () => {
			if (state.isFinished || state.isSubmitting || state.isStreaming) return
			if (state.isRecording) {
				stopRecord()
				return
			}
			if (state.isTranscribing) return
			startRecord()
		}

		const toggleInputMode = () => {
			if (inputModeToggleDisabled.value) return
			state.inputMode = state.inputMode === 'text' ? 'voice' : 'text'
		}

		const handleTextSubmit = async () => {
			if (!canSendText.value) return
			const nextText = toText(state.textDraft)
			if (!nextText) return
			state.textDraft = ''
			await submitStudentAnswer({
				text: nextText,
				fileId: '',
				source: 'text',
			})
		}

		watch(() => state.textDraft, () => {
			if (isTextMode.value) {
				resizeTextComposerField()
			}
		})

		watch(isTextMode, (value) => {
			if (value) {
				resizeTextComposerField()
				return
			}

			const element = textComposerFieldRef.value
			if (!element) return
			element.style.height = '28px'
			element.style.overflowY = 'hidden'
		})

		const toggleAiSpeech = (item, autoPlay = false) => {
			const text = getPlayableAiText(item)
			if (!ttsPlayer.value || !item || item.role !== 'ai' || !text) return
			stopRecordMessagePlayback()
			const ttsKey = getAiReplayTtsKey(item)
			const customerId = getAiCustomerId(item)
			if (!autoPlay && isAiSpeechActive(item)) {
				ttsPlayer.value.stop()
				return
			}
			ttsPlayer.value.send(text, ttsKey, {
				channelId: ttsKey,
				customerId,
				isStream: false,
				isEnd: true,
				isClick: !autoPlay,
			})
			if (!autoPlay && !state.speechSupported) {
				Toast.fail('当前环境不支持 TTS 播放')
			}
		}

		const toggleStudentSpeech = async (item) => {
			const voiceFileId = toText(item && item.voiceFileId)
			if (!item || item.role !== 'student' || !voiceFileId) return

			if (activeRecordMessageId.value === item.id) {
				stopRecordMessagePlayback()
				return
			}

			try {
				stopSpeechPlayback()
				const player = ensureRecordMessagePlayer()
				const audioUrl = getRecordAudioUrl(voiceFileId)
				if (!audioUrl) {
					throw new Error('missing audio url')
				}

				const response = await fetch(audioUrl)
				if (!response.ok) {
					throw new Error(`audio request failed: ${response.status}`)
				}

				const arrayBuffer = await response.arrayBuffer()
				if (!arrayBuffer || !arrayBuffer.byteLength) {
					throw new Error('empty audio buffer')
				}

				const sampleRate = `${state.sampleRate || '00'}`.endsWith('1') ? 16000 : 8000
				stopRecordMessagePlayback()
				activeRecordMessageId.value = item.id
				player.append(arrayBuffer, true, item.id, {
					format: 'pcm',
					encoding: 16,
					channels: 1,
					sampleRate,
				})
				player.append(null, true, item.id)
			} catch (error) {
				activeRecordMessageId.value = ''
				Toast.fail('录音内容播放失败')
			}
		}

		const startCountdown = () => {
			clearCountdownTimer()
			countdownTimer.value = setInterval(() => {
				if (state.combatTimer > 0) {
					state.combatTimer -= 1
					return
				}

				clearCountdownTimer()
				state.isFinished = true
				state.talkTip = '演练时间已到，本轮页面交互已结束。'
				stopSpeechPlayback()
				if (recorder.value) {
					recorder.value.cancel()
				}
				Toast('演练时间已到')
			}, 1000)
		}

		const fetchAsrProvider = async () => {
			try {
				const response = await GetRoadshowAsrProvider({})
				const data = getResponseData(response)
				const sampleRate = toText(data)
				if (sampleRate) {
					state.sampleRate = sampleRate
				}
			} catch (error) {
				state.sampleRate = '00'
			}
		}

		const loadDetail = async () => {
			if (!activityId.value) {
				state.loaded = true
				Toast.fail('缺少活动 ID，无法进入路演实战')
				return
			}

			state.loading = true
			try {
				const detailResponse = await GetActivityDetail({
					activityId: activityId.value,
				})
				const detailData = getResponseData(detailResponse) || {}
				state.activity = normalizeActivity(detailData.activity || detailData.Activity || {})

				const durationMinutes = Number(state.activity.recommendedDurationMinutes || 0)
				const totalSeconds = durationMinutes > 0 ? durationMinutes * 60 : DEFAULT_COMBAT_SECONDS

				if (!state.practiceRecordId) {
					throw new Error('创建演练记录失败，未返回有效记录 ID')
				}

				let needOpenTurn = true

				try {
					const practiceResponse = await GetPracticeDetail({
						practiceRecordId: state.practiceRecordId,
					})
					const practiceData = getResponseData(practiceResponse) || {}
					const details = practiceData.Details || practiceData.details || []
					const practiceStatus = toText(practiceData.PracticeStatus ?? practiceData.practiceStatus)

					if (practiceStatus === 'completed') {
						state.isFinished = true
						state.talkTip = '本轮实战演练已完成。'
						needOpenTurn = false
					}

					if (Array.isArray(details) && details.length > 0) {
						state.chatStream = details.map(mapDetailToChatItem)
						restoreCurrentAiRoleProfileFromHistory()

						const lastAiIndex = state.chatStream.map((item, idx) => ({ item, idx }))
							.filter(({ item }) => item.role === 'ai')
							.reduce((acc, { idx }) => idx, -1)

						if (lastAiIndex >= 0) {
							state.currentDetailId = state.chatStream[lastAiIndex].id
						}

						const lastItem = state.chatStream[state.chatStream.length - 1]
						if (lastItem && lastItem.role === 'student') {
							needOpenTurn = true
						} else {
							needOpenTurn = false
							if (!state.isFinished) {
								state.talkTip = '点击开始录音，继续回答客户问题。'
							}
						}

						state.combatTimer = computeRecoveredTimer(details, totalSeconds)
					} else {
						state.combatTimer = totalSeconds
					}
				} catch (practiceError) {
					state.combatTimer = totalSeconds
				}

				initTtsPlayer()
				initRecorder()
				startCountdown()

				if (needOpenTurn && !state.isFinished) {
					await openPracticeTurn()
				} else {
					scrollChatToBottom()
				}
			} catch (error) {
				Toast.fail(toText(error && error.message) || '路演实战加载失败，请稍后重试')
			} finally {
				state.loading = false
				state.loaded = true
			}
		}

		onMounted(async () => {
			await fetchAsrProvider()
			await loadDetail()
		})

		onBeforeUnmount(() => {
			clearCountdownTimer()
			clearResultMonitor()
			clearTypewriterTimer()
			if (streamController.value) {
				streamController.value.abort()
				streamController.value = null
			}
			if (recorder.value) {
				recorder.value.close()
				recorder.value = null
			}
			if (ttsPlayer.value) {
				ttsPlayer.value.close()
				ttsPlayer.value = null
			}
			if (recordMessagePlayer.value) {
				recordMessagePlayer.value.close(false)
				recordMessagePlayer.value = null
			}
		})

		return {
			isPc,
			chatScrollerRef,
			textComposerFieldRef,
			countdownText,
			countdownParts,
			hasCurrentAiRoleProfile,
			currentAiRoleName,
			currentAiRolePhotoUrl,
			currentAiRoleAvatar,
			getChatAvatarPhotoUrl,
			getChatAvatarName,
			getChatAvatarText,
			showAiLoadingBubble,
			showRecordingPreview,
			isTextMode,
			buttonText,
			micDisabled,
			textInputDisabled,
			canSendText,
			toggleModeClass,
			toggleModeLabel,
			inputModeToggleDisabled,
			textPlaceholder,
			textMaxLength: TEXT_MAX_LENGTH,
			handleMicButtonClick,
			toggleInputMode,
			handleTextDraftInput,
			handleTextSubmit,
			toggleAiSpeech,
			isAiSpeechActive,
			toggleStudentSpeech,
			activeSpeechMessageId,
			activeRecordMessageId,
			...toRefs(state),
		}
	},
}
</script>

<style lang="less" scoped>
.combat-page {
	min-height: 100vh;
	background:
		radial-gradient(circle at top right, rgba(96, 215, 255, 0.2), transparent 30%),
		linear-gradient(180deg, #eef2f7 0%, #edf2f7 100%);
	color: #0f172a;
}

.combat-main {
	padding: 0;
}

.combat-container {
	display: flex;
	flex-direction: column;
	min-height: 100vh;
	height: 100vh;
	overflow: hidden;
}

.combat-countdown {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	padding: 12px 16px;
	background: rgba(255, 255, 255, 0.96);
	border-bottom: 1px solid rgba(148, 163, 184, 0.18);
	flex-shrink: 0;
	position: sticky;
	top: 0;
	z-index: 20;
	box-shadow: 0 4px 14px rgba(15, 23, 42, 0.03);
	backdrop-filter: blur(12px);
}

.combat-topbar-main {
	display: flex;
	align-items: center;
	min-width: 0;
	flex: 1;
}

.combat-role-profile {
	display: flex;
	align-items: center;
	gap: 10px;
	min-width: 0;
}

.combat-role-photo-wrap {
	flex-shrink: 0;
}

.combat-role-photo {
	width: 38px;
	height: 38px;
	border-radius: 50%;
	object-fit: cover;
	background: #e2e8f0;
	box-shadow: 0 8px 20px rgba(15, 23, 42, 0.12);
}

.combat-role-photo-fallback {
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 15px;
	font-weight: 800;
	color: #2563eb;
	background: linear-gradient(135deg, #dbeafe 0%, #eff6ff 100%);
}

.combat-role-copy {
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
}

.combat-role-label {
	font-size: 11px;
	line-height: 1.2;
	color: #64748b;
}

.combat-role-name {
	font-size: 14px;
	line-height: 1.3;
	font-weight: 700;
	color: #0f172a;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 180px;
}

.timer-chip {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	gap: 4px;
	padding: 5px 8px;
	border-radius: 14px;
	background: rgba(37, 99, 235, 0.06);
	border: 1px solid rgba(37, 99, 235, 0.08);
	box-shadow:
		inset 0 1px 0 rgba(255, 255, 255, 0.5),
		0 2px 8px rgba(15, 23, 42, 0.03);
	color: #475569;
	flex-shrink: 0;
}

.timer-chip.compact {
	padding-left: 10px;
	padding-right: 10px;
}

.timer-chip-part {
	min-width: 22px;
	padding: 2px 0;
	border-radius: 8px;
	background: rgba(255, 255, 255, 0.66);
	font-size: 15px;
	line-height: 1;
	font-weight: 700;
	letter-spacing: 0.5px;
	text-align: center;
	font-variant-numeric: tabular-nums;
	font-family: 'DIN Alternate', 'Bahnschrift', 'Segoe UI', sans-serif;
}

.timer-chip-separator {
	font-size: 14px;
	line-height: 1;
	font-weight: 600;
	opacity: 0.55;
}

.chat-window {
	flex: 1;
	display: flex;
	flex-direction: column;
	min-height: 0;
	overflow: hidden;
}

.chat-scroller {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	display: flex;
	flex-direction: column;
	gap: 16px;
	padding: 8px 8px calc(100px + env(safe-area-inset-bottom));
	scroll-behavior: smooth;
	-webkit-overflow-scrolling: touch;
}

.chat-node {
	display: flex;
	flex-direction: column;
}

.status-bubble {
	display: inline-flex;
	align-self: center;
	padding: 8px 12px;
	border-radius: 999px;
	background: rgba(255, 255, 255, 0.8);
	border: 1px solid #dbeafe;
	font-size: 12px;
	line-height: 1.5;
	color: #64748b;
}

.chat-item {
	display: flex;
	gap: 5px;
	align-items: flex-start;

	&.student {
		flex-direction: row-reverse;

		.bubble-wrap {
			align-items: flex-end;
		}

		.sender-name {
			text-align: right;
		}
	}
}

.avatar-circle {
	width: 36px;
	height: 36px;
	border-radius: 50%;
	overflow: hidden;
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	box-shadow: 0 8px 18px rgba(15, 23, 42, 0.08);
	background: linear-gradient(135deg, #e0f2fe 0%, #dbeafe 100%);
	color: #2563eb;

	&.student {
		background: linear-gradient(135deg, #2563eb 0%, #3b82f6 100%);
		color: #ffffff;
	}
}

.avatar-photo {
	width: 100%;
	height: 100%;
	object-fit: cover;
	display: block;
}

.avatar-text {
	font-size: 14px;
	font-weight: 800;
	line-height: 1;
}

.bubble-wrap {
	width: 78vw;
	max-width: 520px;
	display: flex;
	flex-direction: column;
	gap: 0;
}

.sender-name {
	display: none;
}

.message-bubble {
	padding: 12px 14px 10px;
	border-radius: 18px;
	box-shadow: 0 10px 22px rgba(15, 23, 42, 0.06);
	position: relative;

	&.ai {
		background: #ffffff;
		border: 1px solid #e2e8f0;
		padding-right: 48px;
	}

	&.student {
		background: linear-gradient(135deg, #2563eb 0%, #3b82f6 100%);
		color: #ffffff;
		padding-left: 48px;

		&.no-voice {
			padding-left: 14px;
		}
	}

	&.pending {
		opacity: 0.92;
	}
}

.loading-bubble {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	min-height: 46px;
	padding-right: 14px !important;
}

.loading-bubble-student {
	min-height: 22px;
	padding-right: 0 !important;
	justify-content: center;
}

.loading-dot {
	width: 8px;
	height: 8px;
	border-radius: 50%;
	background: #94a3b8;
	animation: loadingBreath 1.4s ease-in-out infinite;

	&:nth-child(2) {
		animation-delay: 0.2s;
	}

	&:nth-child(3) {
		animation-delay: 0.4s;
	}
}

.loading-dot-light {
	background: rgba(255, 255, 255, 0.92);
}

@keyframes loadingBreath {
	0%,
	80%,
	100% {
		transform: scale(0.72);
		opacity: 0.35;
	}

	40% {
		transform: scale(1);
		opacity: 1;
	}
}

.message-content {
	font-size: 14px;
	line-height: 1.75;
	white-space: pre-wrap;
	word-break: break-word;
}

.voice-icon-btn {
	position: absolute;
	top: 10px;
	right: 10px;
	width: 28px;
	height: 28px;
	padding: 0;
	border: none;
	border-radius: 999px;
	background: rgba(14, 165, 233, 0.1);
	display: flex;
	align-items: center;
	justify-content: center;

	.message-bubble.student & {
		left: 10px;
		right: auto;
		background: rgba(255, 255, 255, 0.18);
		box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.28);
	}
}

.voice-icon-mark {
	position: relative;
	width: 18px;
	height: 16px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
}

.voice-arc {
	position: absolute;
	top: 50%;
	left: 0;
	border: 2px solid transparent;
	border-left: none;
	border-radius: 0 999px 999px 0;
	transform: translateY(-50%);
	opacity: 0.7;
	border-right-color: #0284c7;

	.message-bubble.student & {
		left: auto;
		right: 0;
		border-left: 2px solid rgba(255, 255, 255, 0.96);
		border-right: none;
		border-radius: 999px 0 0 999px;
		transform: translateY(-50%);
	}
}

.arc-1 {
	width: 7px;
	height: 7px;
}

.arc-2 {
	width: 11px;
	height: 11px;
}

.arc-3 {
	width: 15px;
	height: 15px;
}

.voice-icon-btn.active .voice-arc {
	animation: voiceArcPulse 1s ease-in-out infinite;
}

.voice-icon-btn.active .arc-2 {
	animation-delay: 0.18s;
}

.voice-icon-btn.active .arc-3 {
	animation-delay: 0.36s;
}

.talk-dock-panel {
	padding: 8px 10px calc(10px + env(safe-area-inset-bottom));
	background: rgba(255, 255, 255, 0.96);
	border-top: 1px solid rgba(148, 163, 184, 0.18);
	position: sticky;
	bottom: 0;
	z-index: 20;
	backdrop-filter: blur(10px);
}

.talk-dock-shell {
	display: flex;
	align-items: center;
	gap: 8px;
}

.input-mode-btn {
	width: 40px;
	height: 40px;
	flex-shrink: 0;
	border: none;
	border-radius: 12px;
	padding: 5px 4px;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 3px;
	color: #0f172a;
	background: linear-gradient(180deg, rgba(255, 255, 255, 0.96) 0%, #eef4fb 100%);
	box-shadow:
		inset 0 0 0 1px rgba(148, 163, 184, 0.16),
		0 8px 18px rgba(15, 23, 42, 0.06);

	&.voice-mode {
		color: #0f766e;
	}

	&.text-mode {
		color: #2563eb;
	}

	&:disabled {
		color: #94a3b8;
		background: #f1f5f9;
		box-shadow: inset 0 0 0 1px rgba(203, 213, 225, 0.8);
	}
}

.input-mode-icon {
	position: relative;
	display: inline-flex;
	flex-shrink: 0;

	&.voice-mode {
		width: 12px;
		height: 14px;
		border: 1.8px solid currentColor;
		border-bottom: none;
		border-radius: 999px 999px 8px 8px;

		&::before {
			content: '';
			position: absolute;
			left: 50%;
			bottom: -4px;
			transform: translateX(-50%);
			width: 1.8px;
			height: 5px;
			border-radius: 999px;
			background: currentColor;
		}

		&::after {
			content: '';
			position: absolute;
			left: 50%;
			bottom: -7px;
			transform: translateX(-50%);
			width: 8px;
			height: 4px;
			border: 1.8px solid currentColor;
			border-top: none;
			border-radius: 0 0 999px 999px;
		}
	}

	&.text-mode {
		width: 13px;
		height: 10px;
		border: 1.5px solid currentColor;
		border-radius: 3px;

		&::before {
			content: '';
			position: absolute;
			left: 1.5px;
			right: 1.5px;
			top: 2px;
			height: 1.5px;
			background: currentColor;
			box-shadow: 0 3px 0 currentColor, 0 6px 0 currentColor;
		}
	}
}

.input-mode-label {
	font-size: 8px;
	font-weight: 800;
	line-height: 1;
	letter-spacing: 0.2px;
}

.talk-input-body {
	flex: 1;
	min-width: 0;
	display: flex;
	align-items: stretch;
}

.h5-mic-btn {
	width: 100%;
	height: 36px;
	border: none;
	border-radius: 11px;
	font-size: 12px;
	font-weight: 800;
	color: #ffffff;
	box-shadow: 0 8px 14px rgba(37, 99, 235, 0.16);

	&.idle {
		background: linear-gradient(135deg, #0f766e 0%, #14b8a6 100%);
	}

	&.recording {
		background: linear-gradient(135deg, #ef4444 0%, #f97316 100%);
	}

	&:disabled {
		background: #cbd5e1;
		box-shadow: none;
	}
}

.text-composer {
	flex: 1;
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 6px 8px;
	border-radius: 12px;
	background: linear-gradient(180deg, #fbfdff 0%, #f8fafc 100%);
	box-shadow:
		inset 0 0 0 1px rgba(148, 163, 184, 0.16),
		0 8px 18px rgba(15, 23, 42, 0.04);

	&.disabled {
		background: #f1f5f9;
	}
}

.text-composer-field {
	flex: 1;
	min-width: 0;
	min-height: 28px;
	max-height: 100px;
	padding: 6px 2px 4px;
	border: none;
	background: transparent;
	resize: none;
	outline: none;
	overflow-y: hidden;
	font-size: 12px;
	line-height: 1.35;
	color: #0f172a;

	&::placeholder {
		color: #94a3b8;
	}

	&:disabled {
		color: #94a3b8;
	}
}

.text-send-btn {
	align-self: center;
	height: 28px;
	padding: 0 10px;
	border: none;
	border-radius: 9px;
	background: linear-gradient(135deg, #2563eb 0%, #3b82f6 100%);
	color: #ffffff;
	font-size: 11px;
	font-weight: 800;
	flex-shrink: 0;
	box-shadow: 0 6px 12px rgba(37, 99, 235, 0.14);

	&:disabled {
		background: #cbd5e1;
		box-shadow: none;
	}
}

@keyframes voiceArcPulse {
	0%,
	100% {
		opacity: 0.35;
		transform: translateY(-50%) scale(0.92);
	}

	50% {
		opacity: 1;
		transform: translateY(-50%) scale(1.08);
	}
}

@media (min-width: 768px) {
	.combat-countdown {
		padding: 14px 20px;
	}

	.timer-chip-part {
		min-width: 32px;
		font-size: 22px;
	}

	.bubble-wrap {
		max-width: 560px;
	}

	.talk-dock-panel {
		border-radius: 24px 24px 0 0;
	}
}
</style>
